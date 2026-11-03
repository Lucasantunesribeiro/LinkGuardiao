using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace LinkGuardiao.Application.Security
{
    /// <summary>
    /// Classifica enderecos IP como publicos (roteaveis na internet) ou nao.
    /// Usado para impedir que links apontem para loopback, redes privadas,
    /// link-local (incluindo o endpoint de metadata 169.254.169.254), CGNAT,
    /// multicast e faixas reservadas/documentacao.
    /// </summary>
    public static class IpAddressPolicy
    {
        private static readonly (uint Network, int PrefixLength)[] BlockedIpv4Ranges =
        {
            (Ipv4(0, 0, 0, 0), 8),         // "this network" (inclui 0.0.0.0)
            (Ipv4(10, 0, 0, 0), 8),        // privado RFC 1918
            (Ipv4(100, 64, 0, 0), 10),     // CGNAT RFC 6598
            (Ipv4(127, 0, 0, 0), 8),       // loopback
            (Ipv4(169, 254, 0, 0), 16),    // link-local (metadata de cloud)
            (Ipv4(172, 16, 0, 0), 12),     // privado RFC 1918
            (Ipv4(192, 0, 0, 0), 24),      // IETF protocol assignments
            (Ipv4(192, 0, 2, 0), 24),      // TEST-NET-1
            (Ipv4(192, 88, 99, 0), 24),    // 6to4 relay anycast (obsoleto)
            (Ipv4(192, 168, 0, 0), 16),    // privado RFC 1918
            (Ipv4(198, 18, 0, 0), 15),     // benchmarking
            (Ipv4(198, 51, 100, 0), 24),   // TEST-NET-2
            (Ipv4(203, 0, 113, 0), 24),    // TEST-NET-3
            (Ipv4(224, 0, 0, 0), 4),       // multicast
            (Ipv4(240, 0, 0, 0), 4),       // reservado + broadcast 255.255.255.255
        };

        /// <summary>
        /// Retorna true somente para enderecos unicast globais.
        /// Enderecos IPv6 que embutem IPv4 (mapped ::ffff:0:0/96) sao avaliados pelo IPv4 embutido.
        /// </summary>
        public static bool IsPublic(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return IsPublicIpv4(ToUInt32(address));
            }

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return false;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                return IsPublicIpv4(ToUInt32(address.MapToIPv4()));
            }

            if (address.ScopeId != 0)
            {
                return false;
            }

            var bytes = address.GetAddressBytes();

            // Apenas global unicast 2000::/3. Isso exclui ::, ::1, ::/96 (IPv4-compatible),
            // 64:ff9b::/96 (NAT64), fc00::/7 (ULA), fe80::/10 (link-local), fec0::/10 e ff00::/8.
            if ((bytes[0] & 0xE0) != 0x20)
            {
                return false;
            }

            // 2001::/23: bloco IANA de proposito especial (Teredo 2001::/32, benchmarking, ORCHID).
            if (bytes[0] == 0x20 && bytes[1] == 0x01 && (bytes[2] & 0xFE) == 0x00)
            {
                return false;
            }

            // 2001:db8::/32: documentacao.
            if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
            {
                return false;
            }

            // 2002::/16: 6to4 embute IPv4 arbitrario (ex.: 2002:7f00:1:: => 127.0.0.1).
            if (bytes[0] == 0x20 && bytes[1] == 0x02)
            {
                return false;
            }

            // 3fff::/20: documentacao (RFC 9637).
            if (bytes[0] == 0x3F && bytes[1] == 0xFF && (bytes[2] & 0xF0) == 0x00)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Interpreta um host como IPv4 seguindo as regras do WHATWG URL Standard
        /// (o mesmo algoritmo que os navegadores usam): partes decimais, octais (0 inicial)
        /// ou hexadecimais (0x), com 1 a 4 partes e a ultima ocupando os bytes restantes.
        /// Ex.: "2130706433", "0x7f000001", "127.1", "0177.0.0.1" => 127.0.0.1.
        /// </summary>
        public static bool TryParseWhatwgIpv4(string host, out IPAddress? address)
        {
            address = null;
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            var parts = host.Split('.');
            if (parts.Length > 1 && parts[^1].Length == 0)
            {
                parts = parts[..^1];
            }

            if (parts.Length is 0 or > 4)
            {
                return false;
            }

            var numbers = new ulong[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!TryParseIpv4Number(parts[i], out numbers[i]))
                {
                    return false;
                }
            }

            for (var i = 0; i < numbers.Length - 1; i++)
            {
                if (numbers[i] > 255)
                {
                    return false;
                }
            }

            var remainingBytes = 5 - numbers.Length;
            if (numbers[^1] >= 1UL << (8 * remainingBytes))
            {
                return false;
            }

            ulong value = numbers[^1];
            for (var i = 0; i < numbers.Length - 1; i++)
            {
                value += numbers[i] << (8 * (3 - i));
            }

            address = new IPAddress(new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            });
            return true;
        }

        /// <summary>
        /// WHATWG "ends in a number": se o ultimo rotulo for numerico (decimal ou 0x...),
        /// o navegador trata o host como IPv4 (e falha se nao for um IPv4 valido).
        /// </summary>
        public static bool EndsInNumber(string host)
        {
            var parts = host.Split('.');
            var last = parts[^1];
            if (last.Length == 0)
            {
                if (parts.Length == 1)
                {
                    return false;
                }

                last = parts[^2];
            }

            if (last.Length > 0 && last.All(char.IsAsciiDigit))
            {
                return true;
            }

            return TryParseIpv4Number(last, out _);
        }

        private static bool TryParseIpv4Number(string input, out ulong value)
        {
            value = 0;
            if (input.Length == 0)
            {
                return false;
            }

            var radix = 10;
            var digits = input;
            if (digits.Length >= 2 && digits[0] == '0' && (digits[1] == 'x' || digits[1] == 'X'))
            {
                radix = 16;
                digits = digits[2..];
            }
            else if (digits.Length >= 2 && digits[0] == '0')
            {
                radix = 8;
                digits = digits[1..];
            }

            if (digits.Length == 0)
            {
                return true;
            }

            foreach (var c in digits)
            {
                var ok = radix switch
                {
                    16 => char.IsAsciiHexDigit(c),
                    8 => c is >= '0' and <= '7',
                    _ => char.IsAsciiDigit(c)
                };
                if (!ok)
                {
                    return false;
                }

                var digit = (ulong)int.Parse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                // Satura acima de 32 bits (ja invalido para IPv4) sem abortar: os digitos
                // restantes ainda precisam ser validados (ex.: "99999999999z" nao e numero).
                value = Math.Min(value * (ulong)radix + digit, (ulong)uint.MaxValue + 1);
            }

            return true;
        }

        private static bool IsPublicIpv4(uint value)
        {
            foreach (var (network, prefixLength) in BlockedIpv4Ranges)
            {
                var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
                if ((value & mask) == network)
                {
                    return false;
                }
            }

            return true;
        }

        private static uint ToUInt32(IPAddress address)
        {
            var b = address.GetAddressBytes();
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        private static uint Ipv4(byte a, byte b, byte c, byte d)
        {
            return ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;
        }
    }
}
