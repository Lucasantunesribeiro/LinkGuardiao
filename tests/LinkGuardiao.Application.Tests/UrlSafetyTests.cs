using System.Net;
using LinkGuardiao.Application.Security;
using Xunit;

namespace LinkGuardiao.Application.Tests
{
    public class UrlSafetyTests
    {
        [Theory]
        [InlineData("https://example.com", "https://example.com/")]
        [InlineData("  https://example.com/path?q=1#frag  ", "https://example.com/path?q=1#frag")]
        [InlineData("HTTP://Example.COM:80/A", "http://example.com/A")]
        [InlineData("https://example.com:8443/x", "https://example.com:8443/x")]
        [InlineData("https://sub_domain.example.com/", "https://sub_domain.example.com/")]
        [InlineData("http://8.8.8.8/dns", "http://8.8.8.8/dns")]
        [InlineData("http://134744072/", "http://8.8.8.8/")]
        [InlineData("http://[2606:4700:4700::1111]/", "http://[2606:4700:4700::1111]/")]
        [InlineData("https://example.com./", "https://example.com/")]
        public void TryNormalize_AcceptsPublicHttpUrls(string input, string expected)
        {
            Assert.True(UrlSafety.TryNormalize(input, out var normalized, out var reason), reason.ToString());
            Assert.Equal(UrlRejectionReason.None, reason);
            Assert.Equal(expected, normalized);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("JaVaScRiPt:alert(document.cookie)")]
        [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
        [InlineData("file:///etc/passwd")]
        [InlineData("ftp://example.com/file")]
        [InlineData("gopher://example.com:70/_payload")]
        [InlineData("vbscript:msgbox(1)")]
        [InlineData("ws://example.com/socket")]
        [InlineData("mailto:user@example.com")]
        public void TryNormalize_RejectsNonHttpSchemes(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason));
            Assert.Equal(UrlRejectionReason.SchemeNotAllowed, reason);
        }

        [Theory]
        [InlineData("//evil.com/path")]
        [InlineData("/relative/path")]
        [InlineData("example.com")]
        public void TryNormalize_RejectsRelativeOrSchemeRelativeUrls(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out _));
        }

        [Theory]
        [InlineData("https://user@example.com/")]
        [InlineData("https://user:password@example.com/")]
        [InlineData("https://trusted.com@evil.com/")]
        [InlineData("https://:@example.com/")]
        public void TryNormalize_RejectsUserInfo(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason));
            Assert.Equal(UrlRejectionReason.UserInfoNotAllowed, reason);
        }

        [Theory]
        [InlineData("https://example.com/\r\nSet-Cookie: a=b")]
        [InlineData("https://example.com/\nfoo")]
        [InlineData("https://example.com/\tfoo")]
        [InlineData("https://exa\u0000mple.com/")]
        [InlineData("https://example.com\\@evil.com/")]
        [InlineData("https://example.com/a\\b")]
        public void TryNormalize_RejectsControlCharactersAndBackslash(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason));
            Assert.Equal(UrlRejectionReason.InvalidCharacters, reason);
        }

        [Fact]
        public void TryNormalize_RejectsUrlsLongerThanLimit()
        {
            var url = "https://example.com/" + new string('a', UrlSafety.MaxUrlLength);
            Assert.False(UrlSafety.TryNormalize(url, out _, out var reason));
            Assert.Equal(UrlRejectionReason.TooLong, reason);
        }

        [Fact]
        public void TryNormalize_AcceptsUrlAtExactLimit()
        {
            var prefix = "https://example.com/";
            var url = prefix + new string('a', UrlSafety.MaxUrlLength - prefix.Length);
            Assert.True(UrlSafety.TryNormalize(url, out var normalized, out _));
            Assert.Equal(UrlSafety.MaxUrlLength, normalized.Length);
        }

        [Fact]
        public void MaxUrlLength_FitsPersistedColumn()
        {
            // ShortenedLink.OriginalUrl e varchar(2000) no PostgreSQL. Um limite de validacao maior
            // deixava passar URLs que estouravam no INSERT (HTTP 500).
            var column = typeof(LinkGuardiao.Application.Entities.ShortenedLink)
                .GetProperty(nameof(LinkGuardiao.Application.Entities.ShortenedLink.OriginalUrl))!
                .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.MaxLengthAttribute), false)
                .Cast<System.ComponentModel.DataAnnotations.MaxLengthAttribute>()
                .Single();

            Assert.True(UrlSafety.MaxUrlLength <= column.Length,
                $"MaxUrlLength={UrlSafety.MaxUrlLength} > coluna={column.Length}");
        }

        [Fact]
        public void TryNormalize_RejectsWhenEscapingPushesUrlOverLimit()
        {
            // 'ä' vira '%C3%A4' (6 chars): a entrada cabe no limite, a forma persistida nao.
            var prefix = "https://example.com/";
            var url = prefix + new string('\u00e4', (UrlSafety.MaxUrlLength - prefix.Length) / 2);
            Assert.True(url.Length <= UrlSafety.MaxUrlLength);
            Assert.False(UrlSafety.TryNormalize(url, out _, out var reason));
            Assert.Equal(UrlRejectionReason.TooLong, reason);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryNormalize_RejectsEmpty(string? input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason));
            Assert.Equal(UrlRejectionReason.Empty, reason);
        }

        [Theory]
        [InlineData("https://bücher.de/ä?q=é", "https://xn--bcher-kva.de/%C3%A4?q=%C3%A9")]
        [InlineData("https://xn--bcher-kva.de/", "https://xn--bcher-kva.de/")]
        [InlineData("https://BÜCHER.de/", "https://xn--bcher-kva.de/")]
        [InlineData("https://example。com/", "https://example.com/")]
        [InlineData("https://exa­mple.com/", "https://example.com/")]
        [InlineData("https://аpple.com/", "https://xn--pple-43d.com/")]
        [InlineData("https://example.com/a b", "https://example.com/a%20b")]
        public void TryNormalize_ConvertsIdnToPunycodeAndEscapesToAscii(string input, string expected)
        {
            Assert.True(UrlSafety.TryNormalize(input, out var normalized, out var reason), reason.ToString());
            Assert.Equal(expected, normalized);
            Assert.All(normalized, c => Assert.True(c < 128, $"non-ASCII char in {normalized}"));
        }

        [Theory]
        [InlineData("https://xn--zz.com/")]
        [InlineData("https://-bad.example.com/")]
        [InlineData("https://bad-.example.com/")]
        [InlineData("http://999.1.1.1/")]
        [InlineData("http://08.1.1.1/")]
        [InlineData("http://1.2.3.4.5/")]
        [InlineData("http://4294967296/")]
        public void TryNormalize_RejectsInvalidHosts(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason));
            Assert.Contains(reason, new[] { UrlRejectionReason.InvalidHost, UrlRejectionReason.Malformed });
        }

        [Fact]
        public void TryNormalize_RejectsLabelLongerThan63Characters()
        {
            var url = $"https://{new string('a', 64)}.com/";
            Assert.False(UrlSafety.TryNormalize(url, out _, out var reason));
            Assert.Equal(UrlRejectionReason.InvalidHost, reason);
        }

        [Theory]
        [InlineData("http://localhost/")]
        [InlineData("http://LOCALHOST:8080/")]
        [InlineData("http://localhost./")]
        [InlineData("http://app.localhost/")]
        [InlineData("http://printer.local/")]
        [InlineData("http://metadata.google.internal/computeMetadata/v1/")]
        [InlineData("http://router.localdomain/")]
        [InlineData("http://nas.home.arpa/")]
        [InlineData("http://1.0.0.127.in-addr.arpa/")]
        [InlineData("http://intranet/")]
        [InlineData("http://metadata/")]
        public void TryNormalize_RejectsInternalHostNames(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason));
            Assert.Equal(UrlRejectionReason.BlockedHost, reason);
        }

        [Theory]
        // Loopback e representacoes alternativas de 127.0.0.1
        [InlineData("http://127.0.0.1/")]
        [InlineData("http://127.0.0.2:8080/")]
        [InlineData("http://2130706433/")]
        [InlineData("http://0x7f000001/")]
        [InlineData("http://0X7F000001/")]
        [InlineData("http://127.1/")]
        [InlineData("http://0177.0.0.1/")]
        [InlineData("http://0x7f.0.0.1/")]
        [InlineData("http://0x7f.1/")]
        [InlineData("http://017700000001/")]
        [InlineData("http://127.0.0.1./")]
        [InlineData("http://１２７.0.0.1/")]
        [InlineData("http://127。0。0。1/")]
        [InlineData("http://[::1]/")]
        [InlineData("http://[0:0:0:0:0:0:0:1]/")]
        [InlineData("http://[::ffff:127.0.0.1]/")]
        [InlineData("http://[::ffff:7f00:1]/")]
        [InlineData("http://[::127.0.0.1]/")]
        // "Esta rede" / unspecified
        [InlineData("http://0.0.0.0/")]
        [InlineData("http://0/")]
        [InlineData("http://0x/")]
        [InlineData("http://[::]/")]
        // Privados RFC 1918
        [InlineData("http://10.0.0.1/")]
        [InlineData("http://10.255.255.255/")]
        [InlineData("http://172.16.0.1/")]
        [InlineData("http://172.31.255.255/")]
        [InlineData("http://192.168.0.1/")]
        [InlineData("http://192.168.1.1/admin")]
        [InlineData("http://3232235777/")]
        [InlineData("http://[::ffff:192.168.0.1]/")]
        [InlineData("http://[::ffff:10.0.0.1]/")]
        // Link-local / metadata de cloud
        [InlineData("http://169.254.169.254/latest/meta-data/")]
        [InlineData("http://169.254.0.1/")]
        [InlineData("http://2852039166/")]
        [InlineData("http://0xa9fea9fe/")]
        [InlineData("http://[::ffff:169.254.169.254]/")]
        [InlineData("http://[fe80::1]/")]
        [InlineData("http://[fe80::1%25eth0]/")]
        [InlineData("http://[febf::1]/")]
        // CGNAT
        [InlineData("http://100.64.0.1/")]
        [InlineData("http://100.127.255.255/")]
        // ULA, site-local, multicast, NAT64, 6to4, Teredo, documentacao
        [InlineData("http://[fc00::1]/")]
        [InlineData("http://[fd12:3456:789a::1]/")]
        [InlineData("http://[fec0::1]/")]
        [InlineData("http://[ff02::1]/")]
        [InlineData("http://[64:ff9b::7f00:1]/")]
        [InlineData("http://[2002:7f00:1::]/")]
        [InlineData("http://[2001:0:4136:e378::1]/")]
        [InlineData("http://[2001:db8::1]/")]
        // Multicast, reservado, broadcast, documentacao IPv4
        [InlineData("http://224.0.0.1/")]
        [InlineData("http://239.255.255.250/")]
        [InlineData("http://240.0.0.1/")]
        [InlineData("http://255.255.255.255/")]
        [InlineData("http://192.0.2.10/")]
        [InlineData("http://198.51.100.1/")]
        [InlineData("http://203.0.113.5/")]
        [InlineData("http://198.18.0.1/")]
        public void TryNormalize_RejectsNonPublicAddressesInAnyRepresentation(string input)
        {
            Assert.False(UrlSafety.TryNormalize(input, out _, out var reason), input);
            Assert.Equal(UrlRejectionReason.BlockedAddress, reason);
        }

        [Theory]
        [InlineData("172.15.255.255")]
        [InlineData("172.32.0.0")]
        [InlineData("100.63.255.255")]
        [InlineData("100.128.0.0")]
        [InlineData("169.253.255.255")]
        [InlineData("11.0.0.1")]
        [InlineData("1.1.1.1")]
        [InlineData("2606:4700:4700::1111")]
        [InlineData("2a00:1450:4001:80b::200e")]
        [InlineData("::ffff:8.8.8.8")]
        public void IpAddressPolicy_AcceptsPublicAddressesAtRangeBoundaries(string address)
        {
            Assert.True(IpAddressPolicy.IsPublic(IPAddress.Parse(address)), address);
        }

        [Theory]
        [InlineData("2130706433", "127.0.0.1")]
        [InlineData("0x7f000001", "127.0.0.1")]
        [InlineData("127.1", "127.0.0.1")]
        [InlineData("127.0.1", "127.0.0.1")]
        [InlineData("0177.0.0.1", "127.0.0.1")]
        [InlineData("0x7f.0x0.0x0.0x1", "127.0.0.1")]
        [InlineData("192.168.257", "192.168.1.1")]
        [InlineData("0x", "0.0.0.0")]
        [InlineData("8.8.8.8.", "8.8.8.8")]
        public void TryParseWhatwgIpv4_ParsesBrowserEquivalentForms(string host, string expected)
        {
            Assert.True(IpAddressPolicy.TryParseWhatwgIpv4(host, out var address));
            Assert.Equal(IPAddress.Parse(expected), address);
        }

        [Theory]
        [InlineData("256.0.0.1")]
        [InlineData("1.2.3.4.5")]
        [InlineData("08.0.0.1")]
        [InlineData("0xg.0.0.1")]
        [InlineData("4294967296")]
        [InlineData("1.2.65536")]
        [InlineData("")]
        public void TryParseWhatwgIpv4_RejectsInvalidForms(string host)
        {
            Assert.False(IpAddressPolicy.TryParseWhatwgIpv4(host, out _));
        }

        [Theory]
        [InlineData("example.99999999999z", false)]
        [InlineData("99999999999z", false)]
        [InlineData("example.0xz", false)]
        [InlineData("example.99999999999", true)]
        [InlineData("example.0x1", true)]
        [InlineData("127.0.0.1.", true)]
        public void EndsInNumber_ValidatesEveryDigitEvenAfterOverflow(string host, bool expected)
        {
            Assert.Equal(expected, IpAddressPolicy.EndsInNumber(host));
        }

        [Fact]
        public void TryNormalize_TreatsOverflowedNonNumericLabelAsDnsName()
        {
            Assert.True(UrlSafety.TryNormalize("https://example.99999999999z/", out var normalized, out var reason), reason.ToString());
            Assert.Equal("https://example.99999999999z/", normalized);
        }

        [Fact]
        public void Describe_ReturnsSpecificMessageForEachReason()
        {
            foreach (var reason in Enum.GetValues<UrlRejectionReason>())
            {
                Assert.False(string.IsNullOrWhiteSpace(UrlSafety.Describe(reason)));
            }

            Assert.NotEqual(
                UrlSafety.Describe(UrlRejectionReason.BlockedAddress),
                UrlSafety.Describe(UrlRejectionReason.SchemeNotAllowed));
        }
    }
}
