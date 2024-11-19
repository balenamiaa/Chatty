using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Chatty.Backend.Tests.Helpers;

public static class TestTokenGenerator
{
    private static readonly byte[] _key = Encoding.UTF8.GetBytes(
        "046b50cb64ce2445dbf199c347271bb7740d82c46d3d3db2f15a90a9fd8541e16afe60c33928c37690df3579570ef6d395fc5b85029285f9116e490154f37afe4dde655be1c4316b38f35687a53c8ec4231976afcd06d2d3189b257ddb8bdeedf13a4a0f594c69c2ecc9d3a98dbe9e61780d5f9020d4e88f54ad9a2986fbe3ea5a7f023bd86ab94812ec68fd37735b229d687d49534765ec8ced10d27c8ee6df6b56787acc92333f2196392bbfb9786537b93b4b70b2213671c2c54205c2e241a691281bb4900397d26a4a636b9433ef9425c02fbd3ce95aac50ce5e2c772939131076b1f1428612ce0173a40a289c1b33dc242201f74b0fb692534b79de8bd3");

    public static string GenerateToken(Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com")
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "chatty",
            Audience = "chatty-client",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
