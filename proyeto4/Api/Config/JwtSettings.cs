namespace Api.Config
{
    public class JwtSettings
    {
        public string Key { get; set; } = "ShinApalacha23@SecretKey!";
        public string Issuer { get; set; } = "ApiAuth";
        public string Audience { get; set; } = "ApiUsers";
    }
}
