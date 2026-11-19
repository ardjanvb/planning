using Domain.Entities.Interfaces;

namespace Domain.Entities;

public class AppUser : IEntity
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
