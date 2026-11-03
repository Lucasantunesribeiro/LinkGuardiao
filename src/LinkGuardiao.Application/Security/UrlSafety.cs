using System.Globalization;
using System.Net;

namespace LinkGuardiao.Application.Security
{
    public enum UrlRejectionReason
    {
        None = 0,
        Empty,
        TooLong,
        InvalidCharacters,
        Malformed,
        SchemeNotAllowed,
        UserInfoNotAllowed,
        InvalidPort,
        InvalidHost,
        BlockedHost,
        BlockedAddress
    }

    /// <summary>
    /// Valida e normaliza URLs de destino dos links encurtados.
    /// Regras: apenas http/https, sem credenciais (user@host), sem caracteres de controle
    /// ou barra invertida, host DNS valido (IDN convertido para punycode) ou IP publico.
    /// Hosts internos (localhost, *.local, *.internal, rotulo unico) e IPs nao publicos
    /// (inclusive em representacoes alternativas como 2130706433, 0x7f000001, 127.1 e
    /// [::ffff:127.0.0.1]) sao rejeitados.
    /// </summary>
    public static class UrlSafety
    {
        /// <summary>
        /// Limite da URL normalizada. Igual ao [MaxLength(2000)] de ShortenedLink.OriginalUrl
        /// (coluna varchar(2000) no PostgreSQL): acima disso o INSERT falhava com 500.
        /// </summary>
        public const int MaxUrlLength = 2000;
        private const int MaxHostLength = 253;
        private const int MaxLabelLength = 63;

        private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
        {
            Uri.UriSchemeHttp,
            Uri.UriSchemeHttps
        };

        private static readonly string[] BlockedHostSuffixes =
        {
            ".localhost",
            ".local",
            ".localdomain",
            ".internal",
            ".home.arpa",
            ".arpa"
        };

        private static readonly IdnMapping Idn = new() { AllowUnassigned = false, UseStd3AsciiRules = false };

        public static bool IsSafeHttpUrl(string? value, out Uri? uri)
        {
            uri = null;
            if (!TryNormalize(value, out var normalized, out _))
            {
                return false;
            }

            uri = new Uri(normalized, UriKind.Absolute);
            return true;
        }

        public static bool IsSafeHttpUrl(string? value)
        {
            return TryNormalize(value, out _, out _);
        }

        /// <summary>
        /// Valida a URL e devolve sua forma canonica ASCII (host em punycode, path/query escapados),
        /// adequada para persistir e para o header Location.
        /// </summary>
        public static bool TryNormalize(string? value, out string normalizedUrl, out UrlRejectionReason reason)
        {
            normalizedUrl = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                reason = UrlRejectionReason.Empty;
                return false;
            }

            var trimmed = value.Trim();
            if (trimmed.Length > MaxUrlLength)
            {
                reason = UrlRejectionReason.TooLong;
                return false;
            }

            foreach (var c in trimmed)
            {
                // Controles C0/C1 (CR/LF/TAB/NUL...) permitem header injection e divergencia de parsing;
                // '\' e tratado como '/' pelos navegadores em http(s), o que confunde a autoridade.
                if (char.IsControl(c) || c == '\\')
                {
                    reason = UrlRejectionReason.InvalidCharacters;
                    return false;
                }
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed))
            {
                reason = UrlRejectionReason.Malformed;
                return false;
            }

            if (!AllowedSchemes.Contains(parsed.Scheme))
            {
                reason = UrlRejectionReason.SchemeNotAllowed;
                return false;
            }

            if (!string.IsNullOrEmpty(parsed.UserInfo) || AuthorityContainsAt(trimmed))
            {
                reason = UrlRejectionReason.UserInfoNotAllowed;
                return false;
            }

            if (parsed.Port is <= 0 or > 65535)
            {
                reason = UrlRejectionReason.InvalidPort;
                return false;
            }

            reason = ClassifyHost(parsed, out var asciiHost);
            if (reason != UrlRejectionReason.None)
            {
                return false;
            }

            string candidate;
            try
            {
                var builder = new UriBuilder(parsed) { Host = asciiHost };
                if (parsed.IsDefaultPort)
                {
                    builder.Port = -1;
                }

                candidate = builder.Uri.AbsoluteUri;
            }
            catch (UriFormatException)
            {
                reason = UrlRejectionReason.Malformed;
                return false;
            }

            if (candidate.Length > MaxUrlLength)
            {
                reason = UrlRejectionReason.TooLong;
                return false;
            }

            normalizedUrl = candidate;
            reason = UrlRejectionReason.None;
            return true;
        }

        public static string Describe(UrlRejectionReason reason)
        {
            return reason switch
            {
                UrlRejectionReason.Empty => "URL is required.",
                UrlRejectionReason.TooLong => $"URL must have at most {MaxUrlLength} characters.",
                UrlRejectionReason.InvalidCharacters => "URL contains invalid characters.",
                UrlRejectionReason.SchemeNotAllowed => "Only http and https URLs are allowed.",
                UrlRejectionReason.UserInfoNotAllowed => "URLs with embedded credentials (user@host) are not allowed.",
                UrlRejectionReason.InvalidPort => "URL port is invalid.",
                UrlRejectionReason.InvalidHost => "URL host is invalid.",
                UrlRejectionReason.BlockedHost => "URL host points to a local or internal network name.",
                UrlRejectionReason.BlockedAddress => "URL host points to a private, loopback or reserved IP address.",
                _ => "URL must be a valid http/https address."
            };
        }

        private static UrlRejectionReason ClassifyHost(Uri parsed, out string asciiHost)
        {
            asciiHost = string.Empty;

            switch (parsed.HostNameType)
            {
                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                {
                    var raw = parsed.IdnHost;
                    if (raw.Contains('%') || !IPAddress.TryParse(raw, out var ip))
                    {
                        // Zone id (fe80::1%eth0) ou forma nao reconhecida: rejeita.
                        return UrlRejectionReason.BlockedAddress;
                    }

                    if (!IpAddressPolicy.IsPublic(ip))
                    {
                        return UrlRejectionReason.BlockedAddress;
                    }

                    asciiHost = parsed.HostNameType == UriHostNameType.IPv6 ? $"[{ip}]" : ip.ToString();
                    return UrlRejectionReason.None;
                }
                case UriHostNameType.Dns:
                    return ClassifyDnsHost(parsed, out asciiHost);
                default:
                    return UrlRejectionReason.InvalidHost;
            }
        }

        private static UrlRejectionReason ClassifyDnsHost(Uri parsed, out string asciiHost)
        {
            asciiHost = string.Empty;

            string host;
            try
            {
                // IdnHost aplica o mapeamento IDNA (ex.: digitos fullwidth, '。', soft hyphen)
                // e converte para punycode. Validar sobre ele evita bypass por Unicode.
                host = parsed.IdnHost.ToLowerInvariant();
                // Garante que rotulos xn-- sejam punycode valido.
                _ = Idn.GetUnicode(host.TrimEnd('.'));
            }
            catch (ArgumentException)
            {
                return UrlRejectionReason.InvalidHost;
            }
            catch (UriFormatException)
            {
                return UrlRejectionReason.InvalidHost;
            }

            if (host.EndsWith('.'))
            {
                host = host[..^1];
            }

            if (host.Length == 0 || host.Length > MaxHostLength)
            {
                return UrlRejectionReason.InvalidHost;
            }

            // Navegadores tratam hosts cujo ultimo rotulo e numerico como IPv4
            // (ex.: "127.0.0.1.", "0x7f.1", "１２７.0.0.1"). Se nao for IPv4 valido, a URL e invalida.
            if (IpAddressPolicy.EndsInNumber(host))
            {
                if (!IpAddressPolicy.TryParseWhatwgIpv4(host, out var ip) || ip is null)
                {
                    return UrlRejectionReason.InvalidHost;
                }

                if (!IpAddressPolicy.IsPublic(ip))
                {
                    return UrlRejectionReason.BlockedAddress;
                }

                asciiHost = ip.ToString();
                return UrlRejectionReason.None;
            }

            var labels = host.Split('.');
            if (labels.Length < 2)
            {
                // Rotulo unico (ex.: "intranet", "metadata") so resolve em redes internas.
                return UrlRejectionReason.BlockedHost;
            }

            foreach (var label in labels)
            {
                if (!IsValidLabel(label))
                {
                    return UrlRejectionReason.InvalidHost;
                }
            }

            if (host == "localhost" || BlockedHostSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.Ordinal)))
            {
                return UrlRejectionReason.BlockedHost;
            }

            asciiHost = host;
            return UrlRejectionReason.None;
        }

        private static bool IsValidLabel(string label)
        {
            if (label.Length == 0 || label.Length > MaxLabelLength)
            {
                return false;
            }

            if (label[0] == '-' || label[^1] == '-')
            {
                return false;
            }

            foreach (var c in label)
            {
                if (!(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-' || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AuthorityContainsAt(string url)
        {
            var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd < 0)
            {
                return false;
            }

            var authorityStart = schemeEnd + 3;
            var authorityEnd = url.IndexOfAny(new[] { '/', '?', '#' }, authorityStart);
            var authority = authorityEnd < 0 ? url[authorityStart..] : url[authorityStart..authorityEnd];
            return authority.Contains('@');
        }
    }
}
