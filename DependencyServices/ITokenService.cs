using System.IdentityModel.Tokens.Jwt;

namespace api_loja_venda_app.DependencyServices
{
    public interface ITokenService
    {
        dynamic GenerateToken();

        dynamic GenerateTokenResetPassword(string email);

        dynamic ValidateToken(string jwtString);

        JwtPayload UnGenereteToken(string jwtString);
    }
}