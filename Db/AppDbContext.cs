using Microsoft.EntityFrameworkCore;

namespace demo_system_user_module.Db;

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

  public override int SaveChanges()
  {
    AddTimestamps();
    return base.SaveChanges();
  }

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    AddTimestamps();
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    AddTimestamps();
    return await base.SaveChangesAsync(cancellationToken);
  }

  public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    AddTimestamps();
    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  private void AddTimestamps()
  {
    var entities = ChangeTracker.Entries()
        .Where(x => x.Entity is Base && (x.State == EntityState.Added || x.State == EntityState.Modified));

    foreach (var entity in entities)
    {
      var now = DateTime.UtcNow; // current datetime

      if (entity.State == EntityState.Added)
      {
        ((Base)entity.Entity).CreatedAt = now;
      }
        ((Base)entity.Entity).UpdatedAt = now;
    }
  }

  public DbSet<User> Users { get; set; }

  public DbSet<Login> Logins { get; set; }

  public DbSet<OneTimeAccessToken> OneTimeAccessTokens { get; set; }


}