using Domain.Entities;
using Framework;

namespace Web.AppState;

public class SessionState
{
    public AppUser? User { get; private set; }
    public bool IsAuthenticated => User is not null;
    public string Role => User?.Role ?? "";
    public string DisplayName => User?.DisplayName ?? "";

    public event Action? Changed;

    public bool CanManageMasterData => AppRoles.CanManageMasterData(Role);
    public bool CanEditPlanning => AppRoles.CanEditPlanning(Role);
    public bool CanApprove => AppRoles.CanApprove(Role);
    public bool CanImport => AppRoles.CanImport(Role);
    public bool CanExport => AppRoles.CanExport(Role);

    public void SetUser(AppUser user)
    {
        User = user;
        Changed?.Invoke();
    }

    public void Logout()
    {
        User = null;
        Changed?.Invoke();
    }

    public string UserNameOrSystem => User?.UserName ?? "system";
}
