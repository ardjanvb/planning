namespace Framework;

public static class AppRoles
{
    public const string AppAdmin = "AppAdmin";
    public const string Planner = "Planner";
    public const string TeamLead = "TeamLead";
    public const string Manager = "Manager";

    public static readonly string[] All =
    [
        AppAdmin,
        Planner,
        TeamLead,
        Manager
    ];

    public static string DisplayName(string role) => role switch
    {
        AppAdmin => "Applicatiebeheerder",
        Planner => "Planner",
        TeamLead => "Teamleider",
        Manager => "Leidinggevende",
        _ => role
    };

    public static bool CanManageMasterData(string role) =>
        role is AppAdmin or Planner;

    public static bool CanEditPlanning(string role) =>
        role is AppAdmin or Planner;

    public static bool CanApprove(string role) =>
        role is AppAdmin or Manager;

    public static bool CanImport(string role) =>
        role is AppAdmin or Planner;

    public static bool CanExport(string role) =>
        role is AppAdmin or Planner or Manager or TeamLead;
}
