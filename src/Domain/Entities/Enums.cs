namespace Domain.Entities;

public enum CalendarDayType
{
    WorkDay = 0,
    NonWorkDay = 1,
    Holiday = 2,
    Closure = 3,
    CollectiveOff = 4,
    SpecialProduction = 5
}

public enum AbsenceType
{
    Vacation = 0,
    Atv = 1,
    Adv = 2,
    SpecialLeave = 3,
    CollectiveOff = 4,
    Other = 5
}

public enum AbsenceStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2
}

public enum WeekPlanStatus
{
    Concept = 0,
    Checked = 1,
    PendingApproval = 2,
    Approved = 3,
    Published = 4,
    Locked = 5
}

public enum ConflictSeverity
{
    Warning = 0,
    Blocking = 1
}
