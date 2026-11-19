using Domain.Contexts;
using Domain.Entities;
using Domain.Seed;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;

namespace Services.Implementations;

public class AuthService(RepositoryContext db) : IAuthService
{
    public async Task<(bool Ok, string Message, AppUser? User)> LoginAsync(string userName, string password)
    {
        var user = await db.AppUsers.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user is null || !user.Active)
            return (false, "Onbekende gebruiker of account inactief.", null);

        if (!DemoSeed.VerifyPassword(password, user.PasswordHash))
            return (false, "Ongeldig wachtwoord.", null);

        return (true, "OK", user);
    }
}
