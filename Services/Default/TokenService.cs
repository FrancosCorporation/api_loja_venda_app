using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using api_loja_venda_app.DependencyServices;
using api_loja_venda_app.Models;

/*
    Gera token utilizando a chave Secret no arquivo Settings
*/
namespace api_loja_venda_app.Services
{
    public class TokenService : ITokenService
    {
        protected readonly IConfiguration _configuration;
        
        public TokenService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public dynamic GenerateToken()
        {
            var appSettingsSection = _configuration.GetSection(nameof(AppSettings));
            var appSettings = appSettingsSection.Get<AppSettings>();
            var securityKey = Encoding.UTF8.GetBytes(appSettings.JWT_SecurityKey);
            var symetricSecurityKey = new SymmetricSecurityKey(securityKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("id", "1"),
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(symetricSecurityKey, SecurityAlgorithms.HmacSha384Signature),
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new{
                token = tokenHandler.WriteToken(token),
                expiration = tokenDescriptor.Expires
            };
        }

        public dynamic GenerateTokenResetPassword(string email)
        {
            var appSettingsSection = _configuration.GetSection(nameof(AppSettings));
            var appSettings = appSettingsSection.Get<AppSettings>();
            var securityKey = Encoding.UTF8.GetBytes(appSettings.JWT_SecurityKey);
            var symetricSecurityKey = new SymmetricSecurityKey(securityKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, email),
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(symetricSecurityKey, SecurityAlgorithms.HmacSha384Signature),
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public dynamic ValidateToken(string jwtString)
        {
            var appSettingsSection = _configuration.GetSection(nameof(AppSettings));
            var appSettings = appSettingsSection.Get<AppSettings>();
            var securityKey = Encoding.UTF8.GetBytes(appSettings.JWT_SecurityKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken validatedToken;
            TokenValidationParameters validationParameters = new TokenValidationParameters()
            {
                ValidateLifetime = true, // Because there is no expiration in the generated token
                ValidateAudience = false, // Because there is no audiance in the generated token
                ValidateIssuer = false,   // Because there is no issuer in the generated token
                ValidIssuer = "Sample",
                ValidAudience = "Sample",
                IssuerSigningKey = new SymmetricSecurityKey(securityKey), // The same key as the one that generate the token
                LifetimeValidator = (DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters) =>
                {
                    if (expires != null)
                    {
                        if (DateTime.Now < expires.Value.ToLocalTime()) return true;
                    }
                    return false;
                }
            };

            try
            {
                var p = tokenHandler.ValidateToken(jwtString, validationParameters, out validatedToken);
                return true;
            }
            catch (SecurityTokenException e)
            {
                Console.Write(e.Message);
                return false;
            }
            
        }

        public JwtPayload UnGenereteToken(string jwtString)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jsonToken = handler.ReadJwtToken(jwtString);
            return jsonToken.Payload;
        }

    }
}