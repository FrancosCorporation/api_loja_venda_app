using System;

namespace api_loja_venda_app.Models
{
    public sealed class TokenResponse
    {
        public string Token { get; init; } = string.Empty;
        public string TokenType { get; init; } = "Bearer";
        public DateTime ExpiresAtUtc { get; init; }
    }
}
