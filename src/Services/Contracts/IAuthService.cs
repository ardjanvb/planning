using Domain.Entities;

namespace Services.Contracts;

public interface IAuthService
{
    Task<(bool Ok, string Message, AppUser? User)> LoginAsync(string userName, string password);
}
