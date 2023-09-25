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
  private Db.AppDbContext _appDbContext;

  public AccessTokenServiceGrpc(ILogger<UserServiceGrpc> logger, IConfiguration config, Db.AppDbContext appDbContext)
  {
    _logger = logger;
    _config = config;
    _appDbContext = appDbContext;
  }

  private AccessTokenReply GenerateAccessToken(dynamic dto)
  {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetValue<string>("JWT_SECRET") ?? ""));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.Aes128CbcHmacSha256);

    dynamic data = new ExpandoObject();
    data.ImageUrl = dto.ImageUrl ?? "";
    data.Id = dto.UserId;


    var claims = new[]
    {
      new Claim(ClaimTypes.NameIdentifier, dto.Username ?? ""),
      new Claim(ClaimTypes.Email, dto.Email ?? ""),
      new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(data)),
    };
    var expiresAt = DateTime.Now;

    switch (dto.DurationType)
    {
      case "10m":
        expiresAt = expiresAt.AddMinutes(10);
        break;
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

    var reply = new AccessTokenReply
    {
      AccessToken = tokenStr,
      ExpiresAtUtc = Timestamp.FromDateTime(expiresAt.ToUniversalTime())
    };
    return reply;
  }

  private VerifyAccessTokenReply VerifyAccessToken(dynamic dto)
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

      SecurityToken validatedToken = null;
      var claimsPrincipal = tokenHandler.ValidateToken(dto.AccessToken, validationParameters, out validatedToken);
      var jwtToken = (JwtSecurityToken)validatedToken;
      var username = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
      var email = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
      dynamic data = JsonConvert.DeserializeObject(jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value ?? "{}")!;

      var role = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;

      return new VerifyAccessTokenReply()
      {
        IsValid = true,
        UserId = data.Id,
        Username = username,
        Email = email,
        ImageUrl = data.ImageUrl,
      };
    }
    catch (Exception)
    {
      return new VerifyAccessTokenReply()
      {
        IsValid = false
      };
    }
  }

  public override Task<AccessTokenReply> GenerateAccessToken(GenerateAccessTokenRequest request, ServerCallContext context)
  {
    dynamic dto = new ExpandoObject();
    dto.Username = request.Username;
    dto.Email = request.Email;
    dto.ImageUrl = request.ImageUrl;
    dto.UserId = request.UserId;
    dto.DurationType = request.DurationType;
    var reply = this.GenerateAccessToken(dto);

    return Task.FromResult(reply);
  }

  public override Task<VerifyAccessTokenReply> VerifyAccessToken(VerifyAccessTokenRequest request, ServerCallContext context)
  {

    dynamic dto = new ExpandoObject();
    dto.AccessToken = request.AccessToken;
    var reply = VerifyAccessToken(dto);
    return Task.FromResult(reply);
  }

  public override async Task<AddOneTimeAccessTokenReply> AddOneTimeAccessToken(AddOneTimeAccessTokenRequest request, ServerCallContext context)
  {
    dynamic dto = new ExpandoObject();
    dto.Username = request.Username;
    dto.Email = request.Email;
    dto.ImageUrl = request.ImageUrl;
    dto.UserId = request.UserId;
    dto.DurationType = "10m";
    var reply = GenerateAccessToken(dto);

    var oneTimeToken = new Db.OneTimeAccessToken
    {
      Email = request.Email,
      Token = reply.AccessToken,
      Username = request.Username,
    };

    _appDbContext.OneTimeAccessTokens.Add(oneTimeToken);
    await _appDbContext.SaveChangesAsync();

    return new AddOneTimeAccessTokenReply
    {
      AccessToken = reply.AccessToken,
      ExpiresAtUtc = reply.ExpiresAtUtc,
    };
  }

  public override Task<VerifyOneTimeAccessTokenReply> VerifyOneTimeAccessToken(VerifyOneTimeAccessTokenRequest request, ServerCallContext context)
  {

    var record = _appDbContext.OneTimeAccessTokens.FirstOrDefault(x => x.Token == request.AccessToken);
    if (record == null)
    {
      return Task.FromResult(new VerifyOneTimeAccessTokenReply
      {
        IsValid = false
      });
    }
    else
    {
      _appDbContext.OneTimeAccessTokens.Remove(record);
      _appDbContext.SaveChanges();
    }

    dynamic dto = new ExpandoObject();
    dto.AccessToken = request.AccessToken;
    var reply = VerifyAccessToken(dto);

    return Task.FromResult(new VerifyOneTimeAccessTokenReply
    {
      IsValid = reply.IsValid,
      UserId = reply.UserId,
      Username = reply.Username,
      Email = reply.Email,
      ImageUrl = reply.ImageUrl,
    });
  }
}