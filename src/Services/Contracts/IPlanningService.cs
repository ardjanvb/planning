using Domain.Entities;
using Services.Models;

namespace Services.Contracts;

public interface IPlanningService
{
    Task<PlanningOverview> GetOverviewAsync(int year, int weekNumber);
    Task<WeekPlan> GetOrCreateWeekPlanAsync(int year, int weekNumber);
    Task<ShiftAssignment> AssignAsync(AssignRequest request, string userName);
    Task RemoveAssignmentAsync(int assignmentId, string userName);
    Task MoveAssignmentAsync(int assignmentId, int newWorkplaceId, DateOnly? newDate, string userName);
    Task CopyWeekAsync(int fromYear, int fromWeek, int toYear, int toWeek, string userName);
    Task<WeekPlan> ChangeStatusAsync(int weekPlanId, WeekPlanStatus newStatus, string userName, string? reason = null);
    Task<IReadOnlyList<ConflictItem>> GetConflictsAsync(int year, int weekNumber);
    Task<IReadOnlyList<WeekPlanSnapshotInfo>> GetSnapshotsAsync(int year, int weekNumber);
    Task<IReadOnlyList<SnapshotAssignmentDto>> GetSnapshotAssignmentsAsync(int snapshotId);
    Task<VersionCompareResult> CompareSnapshotsAsync(int leftSnapshotId, int rightSnapshotId);
}
