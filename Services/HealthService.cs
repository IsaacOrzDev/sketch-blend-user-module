

using Grpc.Core;

namespace demo_system_user_module.Services;

public class HealthServiceGrpc : Health.HealthBase
{
  private readonly ILogger<HealthServiceGrpc> _logger;
  public HealthServiceGrpc(ILogger<HealthServiceGrpc> logger)
  {
    _logger = logger;
  }

  public override Task<HealthCheckResponse> Check(HealthCheckRequest request, ServerCallContext context)
  {
    return Task.FromResult(new HealthCheckResponse
    {
      Status = HealthCheckResponse.Types.ServingStatus.Serving
    });
  }
}