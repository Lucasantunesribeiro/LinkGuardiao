using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LinkGuardiao.Application.Interfaces;
using LinkGuardiao.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LinkGuardiao.Infrastructure.Security
{
    public sealed class LinkAccessGrantService : ILinkAccessGrantService
    {
        private const string GrantTypeClaim = "grant_type";
        private const string ShortCodeClaim = "short_code";
        private const string GrantTypeValue = "link_access";
        private static readonly TimeSpan GrantLifetime = TimeSpan.FromMinutes(1);
        private readonly JwtOptions _options;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public LinkAccessGrantService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string Generate(string shortCode)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: new[]
                {
                    new Claim(GrantTypeClaim, GrantTypeValue),
                    new Claim(ShortCodeClaim, shortCode),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
                },
                notBefore: now,
                expires: now.Add(GrantLifetime),
                signingCredentials: credentials);

            return _tokenHandler.WriteToken(token);
        }

        public bool TryValidate(string shortCode, string accessGrant)
        {
            if (string.IsNullOrWhiteSpace(accessGrant))
            {
                return false;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                ClockSkew = TimeSpan.FromSeconds(10),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret))
            };

            try
            {
                var principal = _tokenHandler.ValidateToken(accessGrant, validationParameters, out _);
                var grantType = principal.FindFirst(GrantTypeClaim)?.Value;
                var grantedShortCode = principal.FindFirst(ShortCodeClaim)?.Value;

                return grantType == GrantTypeValue
                    && string.Equals(grantedShortCode, shortCode, StringComparison.Ordinal);
            }
            catch (SecurityTokenException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
