using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace demo_system_user_module.Services;

public class UserServiceGrpc : UserService.UserServiceBase
{

  private Db.AppDbContext _appDbContext;

  private readonly ILogger<UserServiceGrpc> _logger;
  public UserServiceGrpc(ILogger<UserServiceGrpc> logger, Db.AppDbContext appDbContext)
  {
    _logger = logger;
    _appDbContext = appDbContext;
  }

  public override async Task<UserReply> CreateUser(CreateUserRequest request, ServerCallContext context)
  {
    var user = new Db.User
    {
      Name = request.Name,
      Email = request.Email,
      LoginAt = DateTime.UtcNow,
      Logins = new List<Db.Login>
      {
        new() {
          Method = request.Login.Method,
          Data = JsonDocument.Parse(request.Login.Data.ToString()),
          ImageUrl = request.Login.ImageUrl,

        }
      },
    };
    _appDbContext.Users.Add(user);
    await _appDbContext.SaveChangesAsync();

    return new UserReply
    {
      User = new()
      {
        Id = (uint)user.Id,
        Name = request.Name,
        Email = request.Email,
        ImageUrl = request.Login.ImageUrl,
      }
    };
  }

  public override Task<UserReply> FindUser(FindUserRequest request, ServerCallContext context)
  {
    var user = _appDbContext.Users.Include(u => u.Logins)
    .FirstOrDefault(u => request.Email != null && request.Email != "" && u.Email == request.Email
    ||
      u.Name == request.Name && u.Logins != null &&
       u.Logins.FirstOrDefault(x => x.Method == request.LoginMethod) != null
    );
    if (user == null)
    {
      return Task.FromResult(new UserReply());
    }
    else
    {
      var ImageUrl = user.Logins != null ? user.Logins.FirstOrDefault(x => x.Method == request.LoginMethod)?.ImageUrl : "";
      return Task.FromResult(new UserReply
      {
        User = new()
        {
          Id = (uint)user.Id,
          Name = user.Name,
          Email = user.Email,
          ImageUrl = ImageUrl != null ? ImageUrl : "",
        }
      });
    }
  }

  public override async Task<UserReply> LoginUser(LoginUserRequest request, ServerCallContext context)
  {
    var user = _appDbContext.Users.Include(u => u.Logins).FirstOrDefault(u => u.Id == request.Id);
    if (user == null)
    {
      return new UserReply();
    }

    var login = _appDbContext.Logins.FirstOrDefault(l => l.UserId == request.Id && l.Method == request.Login.Method);
    if (login == null)
    {
      login = new Db.Login
      {
        UserId = (int)request.Id,
        Method = request.Login.Method,
        Data = request.Login.Data == null ? JsonDocument.Parse("{}") : JsonDocument.Parse(request.Login.Data.ToString()),
        ImageUrl = request.Login.ImageUrl,
      };
      _appDbContext.Logins.Add(login);
    }
    else
    {
      login.Data = request.Login.Data == null ? JsonDocument.Parse("{}") : JsonDocument.Parse(request.Login.Data.ToString());
      login.ImageUrl = request.Login.ImageUrl;
    }
    user.LoginAt = DateTime.UtcNow;
    await _appDbContext.SaveChangesAsync();
    return new UserReply
    {
      User = new()
      {
        Id = (uint)user.Id,
        Name = user.Name,
        Email = user.Email,
        ImageUrl = login.ImageUrl,
      }
    };
  }
}
