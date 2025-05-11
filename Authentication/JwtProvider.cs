using dotnetprojekt.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace dotnetprojekt.Authentication
{
    public class JwtProvider
    {
        private readonly JwtOptions _options;
        public JwtProvider(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        } 
        public string GenerateJwtToken(User user)
        {
            var claims = new Claim[]
            {
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()), // JWT ID (losowy identyfikator 128-bit liczba)
                new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),  // Subject (podmiot) - identyfikator użytkownika
                new Claim(JwtRegisteredClaimNames.Email,user.Email.ToString()), // Email
                new Claim(JwtRegisteredClaimNames.Name, user.Username)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
            var SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: System.DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
                signingCredentials: SigningCredentials
            );

            string TokenValue = new JwtSecurityTokenHandler().WriteToken(token);
            
            return TokenValue;

        }
    }
}
