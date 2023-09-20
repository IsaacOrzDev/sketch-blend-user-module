using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
  private readonly IConfiguration configuration;
  private readonly string connectionString;

  public AppDbContext(IConfiguration configuration) : base()
  {
    this.configuration = configuration;
    this.connectionString = this.configuration.GetValue<string>("CONNECTION_STRING") ?? "";
  }

  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    if (connectionString is not null)
      optionsBuilder.UseNpgsql(connectionString);
  }

  public DbSet<User> Users { get; set; }

  public DbSet<Login> Logins { get; set; }

  public DbSet<OneTimeAccessToken> OneTimeAccessTokens { get; set; }
}