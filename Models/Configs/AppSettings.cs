namespace api_loja_venda_app.Models
{
    public class AppSettings 
    {
        public string JWT_SecurityKey {get; set;}
        public string JWT_Issuer {get; set;}
        public string JWT_Audience {get; set;}
    }
}