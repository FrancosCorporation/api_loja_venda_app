using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api_loja_venda_app.DependencyServices;
using api_loja_venda_app.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api_loja_venda_app.Services
{
    public sealed class TokenService : ITokenService
    {
        private const string AccessPurpose = "access";
        private const string PasswordResetPurpose = "password_reset";
        private readonly AppSettings _settings;
        private readonly JwtSecurityTokenHandler _tokenHandler = new() { MapInboundClaims = false };
        private readonly SigningCredentials _signingCredentials;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IOptions<AppSettings> options, ILogger<TokenService> logger)
        {
            _settings = options.Value;
            _logger = logger;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecurityKey));
            _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha384);
        }

        public TokenResponse GenerateToken(string userId, string email)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("A user ID from the authenticated account is required.", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("An email address is required.", nameof(email));
            }

            return CreateToken(
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userId),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                    new Claim("token_use", AccessPurpose)
                },
                TimeSpan.FromMinutes(_settings.AccessTokenMinutes));
        }

        public TokenResponse GenerateTokenResetPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("An email address is required.", nameof(email));
            }

            return CreateToken(
                new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, email),
                    new Claim(JwtRegisteredClaimNames.Email, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                    new Claim("token_use", PasswordResetPurpose)
                },
                TimeSpan.FromMinutes(_settings.PasswordResetTokenMinutes));
        }

        public bool TryValidateToken(string jwtString, TokenPurpose expectedPurpose, out ClaimsPrincipal principal)
        {
            principal = null;

            var expectedPurposeClaim = expectedPurpose switch
            {
                TokenPurpose.Access => AccessPurpose,
                TokenPurpose.PasswordReset => PasswordResetPurpose,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(jwtString) || expectedPurposeClaim is null)
            {
                return false;
            }

            var validationParameters = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecurityKey)),
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha384 }
            };

            try
            {
                var validatedPrincipal = _tokenHandler.ValidateToken(jwtString, validationParameters, out _);
                var purpose = validatedPrincipal.FindFirst("token_use")?.Value;
                if (!string.Equals(purpose, expectedPurposeClaim, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(validatedPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value) ||
                    string.IsNullOrWhiteSpace(validatedPrincipal.FindFirst(JwtRegisteredClaimNames.Email)?.Value))
                {
                    _logger.LogWarning("JWT validation failed its purpose or required-claims check.");
                    return false;
                }

                principal = validatedPrincipal;
                return true;
            }
            catch (SecurityTokenException)
            {
                _logger.LogWarning("JWT validation rejected a token.");
                return false;
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("JWT validation rejected a malformed token.");
                return false;
            }
            catch (FormatException)
            {
                _logger.LogWarning("JWT validation rejected a malformed token.");
                return false;
            }
        }

        private TokenResponse CreateToken(IEnumerable<Claim> claims, TimeSpan lifetime)
        {
            var expiresAtUtc = DateTime.UtcNow.Add(lifetime);
            var token = _tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                NotBefore = DateTime.UtcNow,
                Expires = expiresAtUtc,
                SigningCredentials = _signingCredentials
            });

            return new TokenResponse
            {
                Token = _tokenHandler.WriteToken(token),
                ExpiresAtUtc = expiresAtUtc
            };
        }
    }
}
