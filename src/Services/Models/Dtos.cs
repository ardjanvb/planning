using Domain;
using Domain.Entities;

namespace Services.Models;

public record ConflictItem(
    ConflictSeverity Severity,
    string Code,
    string Message,
    int? EmployeeId = null,
    int? WorkplaceId = null,
    DateOnly? Date = null,
    int? AssignmentId = null);

public record CapacityRow(
    int WorkplaceId,
    string WorkplaceName,
    string ExternalNumber,
    string? DepartmentName,
    int RequiredShifts,
    decimal RequiredHours,
    int PlannedShifts,
    decimal PlannedHours,
    decimal DifferenceHours,
    int AssignedEmployees);

public record ImportPreviewRow(
    int RowNumber,
    int Year,
    int WeekNumber,
    string WorkplaceNumber,
    int? WorkplaceId,
    int RequiredShifts,
    decimal HoursPerShift,
    DateOnly? SpecificDate,
    string? Error);

public record ImportPreviewResult(
    IReadOnlyList<ImportPreviewRow> Rows,
    IReadOnlyList<string> Errors,
    int ValidCount,
    int ErrorCount);

public record ImportConfirmResult(
    int BatchId,
    int ProcessedRows,
    int ErrorCount,
    string DiffSummary);

public record WeekMatrixCell(
    DateOnly Date,
    IReadOnlyList<ShiftAssignmentDto> Assignments,
    decimal RequiredHours,
    decimal PlannedHours,
    bool HasApprovedAbsence = false,
    IReadOnlyList<string>? ClosedShifts = null,
    int? AbsenceId = null)
{
    public bool IsFullyClosed =>
        ClosedShifts is { Count: > 0 } &&
        PlanningConstants.StandardShifts.All(s =>
            ClosedShifts.Any(c => string.Equals(c, s, StringComparison.OrdinalIgnoreCase)));

    public bool CanAssign => !HasApprovedAbsence && !IsFullyClosed;
}

public record ShiftAssignmentDto(
    int Id,
    DateOnly Date,
    int WorkplaceId,
    string WorkplaceName,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? EmployeeId,
    string? EmployeeName,
    decimal PlannedHours,
    string? OverrideReason);

public record WorkplaceWeekRow(
    int WorkplaceId,
    string WorkplaceName,
    string ExternalNumber,
    IReadOnlyDictionary<DateOnly, WeekMatrixCell> Days,
    decimal RequiredHours,
    decimal PlannedHours);

public record EmployeeWeekRow(
    int EmployeeId,
    string EmployeeName,
    decimal ContractHours,
    IReadOnlyDictionary<DateOnly, WeekMatrixCell> Days,
    decimal PlannedHours,
    bool HasApprovedAbsence);

public record PlanningOverview(
    WeekPlan Plan,
    IReadOnlyList<DateOnly> Days,
    IReadOnlyList<WorkplaceWeekRow> ByWorkplace,
    IReadOnlyList<EmployeeWeekRow> ByEmployee,
    IReadOnlyList<CapacityRow> Capacity,
    IReadOnlyList<ConflictItem> Conflicts,
    IReadOnlyDictionary<DateOnly, IReadOnlyList<string>> ClosedShiftsByDate);

public record AssignRequest(
    int WeekPlanId,
    DateOnly Date,
    int WorkplaceId,
    int EmployeeId,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PlannedHours,
    string? OverrideReason);

public record WeekPlanSnapshotInfo(
    int Id,
    int WeekPlanId,
    int Year,
    int WeekNumber,
    int Version,
    WeekPlanStatus Status,
    DateTime CreatedAt,
    string CreatedBy,
    int AssignmentCount);

public record SnapshotAssignmentDto(
    DateOnly Date,
    int WorkplaceId,
    string WorkplaceName,
    int? EmployeeId,
    string? EmployeeName,
    string ShiftName,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal PlannedHours);

public record VersionCompareResult(
    WeekPlanSnapshotInfo Left,
    WeekPlanSnapshotInfo Right,
    IReadOnlyList<SnapshotAssignmentDto> OnlyInLeft,
    IReadOnlyList<SnapshotAssignmentDto> OnlyInRight,
    IReadOnlyList<SnapshotAssignmentDto> Unchanged);
