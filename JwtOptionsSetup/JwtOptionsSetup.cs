using dotnetprojekt.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;


namespace dotnetprojekt.JwtOptionsSetup
{
    public class JwtOptionsSetup : IConfigureOptions<JwtOptions>
    {
        private readonly IConfiguration _configuration;
        private const string SectionName = "Jwt";

        public JwtOptionsSetup(IConfiguration configuration) // dostep do appsettings, zmiennych srodowiskowych itp
        {
            _configuration = configuration;
        }

        public void Configure(JwtOptions options)
        {
            _configuration.GetSection(SectionName).Bind(options);
        }
    }
}
