using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
  public AppDbContext CreateDbContext(string[] args)
  {
    IConfigurationRoot configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

    var builder = new DbContextOptionsBuilder<AppDbContext>();
    var connectionString = configuration.GetValue<String>("CONNECTION_STRING");
    builder.UseNpgsql(connectionString);

    return new AppDbContext(builder.Options);
  }
}