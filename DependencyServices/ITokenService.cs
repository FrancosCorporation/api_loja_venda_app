using System.Security.Claims;
using api_loja_venda_app.Models;

namespace api_loja_venda_app.DependencyServices
{
    public interface ITokenService
    {
        TokenResponse GenerateToken(string userId, string email);

        TokenResponse GenerateTokenResetPassword(string email);

        bool TryValidateToken(string jwtString, TokenPurpose expectedPurpose, out ClaimsPrincipal principal);
    }
}
