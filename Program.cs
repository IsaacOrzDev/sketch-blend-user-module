using demo_system_sub.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("hostsettings.json", optional: true);

var configuration = builder.Configuration
  .SetBasePath(Directory.GetCurrentDirectory())
  .AddJsonFile($"appsettings.json", true, true)
  .AddEnvironmentVariables().Build();

builder.Services.AddSingleton<IConfiguration>(configuration);
builder.Services.AddScoped<AppDbContext, AppDbContext>();

builder.Services.AddGrpc();


var app = builder.Build();

using (var scope = app.Services.CreateScope())

{

  // var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  // if (context is null || !await context.Database.CanConnectAsync())
  // {
  //   Console.WriteLine("Cannot connect to database");
  //   return;
  // }

  // if (!await context.Database.EnsureCreatedAsync())
  // {
  //   await context.Database.MigrateAsync();
  // }
}

app.MapGrpcService<UserService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
