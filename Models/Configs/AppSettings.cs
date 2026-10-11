namespace api_loja_venda_app.Models
{
    public sealed class AppSettings
    {
        public string SecurityKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 60;
        public int PasswordResetTokenMinutes { get; set; } = 120;
    }
}
