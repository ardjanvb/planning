using Domain.Entities;

namespace Services.Contracts;

public interface IMasterDataService
{
    Task<List<Employee>> GetEmployeesAsync(bool includeInactive = false);
    Task<Employee?> GetEmployeeAsync(int id);
    Task<Employee> SaveEmployeeAsync(Employee employee, string userName);
    Task SetEmployeeQualificationsAsync(int employeeId, IEnumerable<int> qualificationIds, string userName);

    Task<List<JobFunction>> GetJobFunctionsAsync();
    Task<JobFunction> SaveJobFunctionAsync(JobFunction item);

    Task<List<Qualification>> GetQualificationsAsync();
    Task<Qualification> SaveQualificationAsync(Qualification item);

    Task<List<Department>> GetDepartmentsAsync();
    Task<Department> SaveDepartmentAsync(Department item);

    Task<List<Crew>> GetCrewsAsync();
    Task<Crew> SaveCrewAsync(Crew item);

    Task<List<CrewPattern>> GetCrewPatternsAsync();
    Task<CrewPattern> SaveCrewPatternAsync(CrewPattern item);
    Task SavePatternDaysAsync(int patternId, IEnumerable<PatternDay> days);

    Task<List<Workplace>> GetWorkplacesAsync(bool includeInactive = false);
    Task<Workplace?> GetWorkplaceAsync(int id);
    Task<Workplace> SaveWorkplaceAsync(Workplace item, string userName);
    Task SaveWorkplaceRequirementsAsync(int workplaceId, IEnumerable<WorkplaceRequirement> requirements);

    Task<List<CalendarDay>> GetCalendarDaysAsync(int year);
    Task UpsertCalendarDayAsync(DateOnly date, CalendarDayType type, string? remark);

    Task<List<WorkplaceCalendarException>> GetWorkplaceCalendarExceptionsAsync(int? workplaceId = null, int? year = null);
    Task UpsertWorkplaceCalendarExceptionAsync(int workplaceId, DateOnly date, CalendarDayType type, string? remark);
    Task DeleteWorkplaceCalendarExceptionAsync(int id);

    Task<List<ShiftClosure>> GetShiftClosuresAsync(DateOnly from, DateOnly to);
    Task SetShiftClosuresForDateAsync(DateOnly date, IEnumerable<string> closedShiftNames, string? remark = null);

    Task<List<Absence>> GetAbsencesAsync(DateOnly? from = null, DateOnly? to = null);
    Task<Absence> SaveAbsenceAsync(Absence absence, string userName);
    /// <summary>Removes approved leave for a single day (deletes, shrinks, or splits the absence).</summary>
    Task RemoveAbsenceForDayAsync(int employeeId, DateOnly date, string userName);
}
