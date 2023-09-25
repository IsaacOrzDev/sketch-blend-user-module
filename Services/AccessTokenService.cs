using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Dynamic;
using Newtonsoft.Json;
using Google.Protobuf.WellKnownTypes;

namespace demo_system_user_module.Services;

public class AccessTokenServiceGrpc : AccessTokenService.AccessTokenServiceBase
{

  private readonly ILogger<UserServiceGrpc> _logger;
  private readonly IConfiguration _config;

  public AccessTokenServiceGrpc(ILogger<UserServiceGrpc> logger, IConfiguration config)
  {
    _logger = logger;
    _config = config;
  }

  public override Task<AccessTokenReply> GenerateAccessToken(GenerateAccessTokenRequest request, ServerCallContext context)
  {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetValue<string>("JWT_SECRET") ?? ""));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.Aes128CbcHmacSha256);

    dynamic data = new ExpandoObject();
    data.ImageUrl = request.ImageUrl ?? "";
    data.Id = request.UserId;


    var claims = new[]
    {
      new Claim(ClaimTypes.NameIdentifier, request.Username ?? ""),
      new Claim(ClaimTypes.Email, request.Email ?? ""),
      new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(data)),
    };
    var expiresAt = DateTime.Now;

    switch (request.DurationType)
    {
      case "1d":
      default:
        expiresAt = expiresAt.AddDays(1);
        break;
    }

    var token = new JwtSecurityToken(_config.GetValue<string>("JWT_ISSUER"),
      _config.GetValue<string>("JWT_AUDIENCE"),
      claims,
      expires: expiresAt,
      signingCredentials: credentials);

    var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

    return Task.FromResult(new AccessTokenReply()
    {
      AccessToken = tokenStr,
      ExpiresAtUtc = Timestamp.FromDateTime(expiresAt.ToUniversalTime())
    });
  }

  public override Task<VerifyAccessTokenReply> VerifyAccessToken(VerifyAccessTokenRequest request, ServerCallContext context)
  {
    var tokenHandler = new JwtSecurityTokenHandler();
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetValue<string>("JWT_SECRET") ?? ""));
    var validationParameters = new TokenValidationParameters
    {
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = securityKey,
      ValidateIssuer = true,
      ValidIssuer = _config.GetValue<string>("JWT_ISSUER"),
      ValidateAudience = true,
      ValidAudience = _config.GetValue<string>("JWT_AUDIENCE"),
      ClockSkew = TimeSpan.Zero
    };

    try
    {
      var claimsPrincipal = tokenHandler.ValidateToken(request.AccessToken, validationParameters, out var validatedToken);
      var jwtToken = (JwtSecurityToken)validatedToken;
      var username = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
      var email = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
      dynamic data = JsonConvert.DeserializeObject(jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value ?? "{}")!;

      var role = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

      return Task.FromResult(new VerifyAccessTokenReply()
      {
        IsValid = true,
        UserId = data.Id,
        Username = username,
        Email = email,
        ImageUrl = data.ImageUrl,
      });
    }
    catch (Exception)
    {
      return Task.FromResult(new VerifyAccessTokenReply()
      {
        IsValid = false
      });
    }
  }
}