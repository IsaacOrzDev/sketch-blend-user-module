using Grpc.Core;

namespace demo_system_sub.Services;

public class UserService : User.UserBase
{

  private AppDbContext _appDbContext;

  private readonly ILogger<UserService> _logger;
  public UserService(ILogger<UserService> logger, AppDbContext appDbContext)
  {
    _logger = logger;
    _appDbContext = appDbContext;
  }

  public override async Task<UserReply> CreateUser(CreateUserRequest request, ServerCallContext context)
  {
    var user = new UserModel
    {
      Name = request.Name,
      Email = request.Email,
    };
    _appDbContext.Users.Add(user);
    await _appDbContext.SaveChangesAsync();

    return new UserReply
    {
      Id = (uint)user.Id,
      Name = request.Name,
      Email = request.Email,
    };
  }

}
