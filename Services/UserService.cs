using Grpc.Core;

namespace demo_system_user_module.Services;

public class UserServiceGrpc : UserService.UserServiceBase
{

  private AppDbContext _appDbContext;

  private readonly ILogger<UserServiceGrpc> _logger;
  public UserServiceGrpc(ILogger<UserServiceGrpc> logger, AppDbContext appDbContext)
  {
    _logger = logger;
    _appDbContext = appDbContext;
  }

  public override async Task<UserReply> CreateUser(CreateUserRequest request, ServerCallContext context)
  {
    var user = new User
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

  public override Task<TestingReply> Testing(TestingRequest request, ServerCallContext context)
  {
    return Task.FromResult(new TestingReply
    {
      Message = "testing"
    });
  }
}
