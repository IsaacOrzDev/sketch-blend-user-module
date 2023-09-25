using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace demo_system_user_module.Db;


public class Base
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }


  public DateTime? CreatedAt { get; set; }

  public DateTime? UpdatedAt { get; set; }

  public Base()
  {
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }
}

public class Login : Base
{
  public required string Method { get; set; }
  public int UserId { get; set; }
  public string? ImageUrl { get; set; }
  public JsonDocument? Data { get; set; }
  public User User { get; set; }
}

public class User : Base
{
  public required string Name { get; set; }
  public string? Email { get; set; }
  public DateTime? LoginAt { get; set; }
  public List<Login>? Logins { get; set; }

}

public class OneTimeAccessToken : Base
{
  public required string Token { get; set; }
  public required string Email { get; set; }
  public string? Username { get; set; }
}