using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Domain.Contexts;

public class RepositoryContext : DbContext
{
    public RepositoryContext(DbContextOptions<RepositoryContext> options) : base(options)
    {
        Database.SetCommandTimeout(3600);
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<JobFunction> JobFunctions => Set<JobFunction>();
    public DbSet<Qualification> Qualifications => Set<Qualification>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<CrewPattern> CrewPatterns => Set<CrewPattern>();
    public DbSet<PatternDay> PatternDays => Set<PatternDay>();
    public DbSet<Crew> Crews => Set<Crew>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeQualification> EmployeeQualifications => Set<EmployeeQualification>();
    public DbSet<EmployeeCrewMembership> EmployeeCrewMemberships => Set<EmployeeCrewMembership>();
    public DbSet<EmployeeChange> EmployeeChanges => Set<EmployeeChange>();
    public DbSet<Workplace> Workplaces => Set<Workplace>();
    public DbSet<WorkplaceRequirement> WorkplaceRequirements => Set<WorkplaceRequirement>();
    public DbSet<CalendarDay> CalendarDays => Set<CalendarDay>();
    public DbSet<WorkplaceCalendarException> WorkplaceCalendarExceptions => Set<WorkplaceCalendarException>();
    public DbSet<ShiftClosure> ShiftClosures => Set<ShiftClosure>();
    public DbSet<Absence> Absences => Set<Absence>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<CapacityNeed> CapacityNeeds => Set<CapacityNeed>();
    public DbSet<WeekPlan> WeekPlans => Set<WeekPlan>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<WeekPlanSnapshot> WeekPlanSnapshots => Set<WeekPlanSnapshot>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<JobFunction>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Qualification>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100);
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<CrewPattern>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<PatternDay>(e =>
        {
            e.Property(x => x.ShiftName).HasMaxLength(50).IsRequired();
            e.Property(x => x.NetHours).HasPrecision(6, 2);
            e.HasOne(x => x.CrewPattern).WithMany(x => x.Days).HasForeignKey(x => x.CrewPatternId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Crew>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.CrewPattern).WithMany(x => x.Crews).HasForeignKey(x => x.CrewPatternId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.PersonnelNumber).IsUnique();
            e.Property(x => x.PersonnelNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ContractHoursPerWeek).HasPrecision(6, 2);
            e.Property(x => x.Fte).HasPrecision(6, 3);
            e.HasOne(x => x.PrimaryJobFunction).WithMany(x => x.Employees).HasForeignKey(x => x.PrimaryJobFunctionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PrimaryCrew).WithMany().HasForeignKey(x => x.PrimaryCrewId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PrimaryWorkplace).WithMany().HasForeignKey(x => x.PrimaryWorkplaceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmployeeQualification>(e =>
        {
            e.HasIndex(x => new { x.EmployeeId, x.QualificationId }).IsUnique();
            e.HasOne(x => x.Employee).WithMany(x => x.Qualifications).HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Qualification).WithMany(x => x.EmployeeQualifications).HasForeignKey(x => x.QualificationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeCrewMembership>(e =>
        {
            e.HasOne(x => x.Employee).WithMany(x => x.CrewMemberships).HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Crew).WithMany(x => x.Memberships).HasForeignKey(x => x.CrewId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeChange>(e =>
        {
            e.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Employee).WithMany(x => x.Changes).HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workplace>(e =>
        {
            e.HasIndex(x => x.ExternalNumber).IsUnique();
            e.Property(x => x.ExternalNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Department).WithMany(x => x.Workplaces).HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkplaceRequirement>(e =>
        {
            e.HasOne(x => x.Workplace).WithMany(x => x.Requirements).HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.JobFunction).WithMany(x => x.WorkplaceRequirements).HasForeignKey(x => x.JobFunctionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Qualification).WithMany(x => x.WorkplaceRequirements).HasForeignKey(x => x.QualificationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CalendarDay>(e =>
        {
            e.HasIndex(x => x.Date).IsUnique();
        });

        modelBuilder.Entity<WorkplaceCalendarException>(e =>
        {
            e.HasIndex(x => new { x.WorkplaceId, x.Date }).IsUnique();
            e.HasOne(x => x.Workplace).WithMany(x => x.CalendarExceptions).HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShiftClosure>(e =>
        {
            e.HasIndex(x => new { x.Date, x.ShiftName }).IsUnique();
            e.Property(x => x.ShiftName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Remark).HasMaxLength(200);
        });

        modelBuilder.Entity<Absence>(e =>
        {
            e.HasOne(x => x.Employee).WithMany(x => x.Absences).HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.CreatedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<ImportBatch>(e =>
        {
            e.Property(x => x.ImportedBy).HasMaxLength(100);
            e.Property(x => x.SourceFileName).HasMaxLength(260);
        });

        modelBuilder.Entity<CapacityNeed>(e =>
        {
            e.Property(x => x.HoursPerShift).HasPrecision(6, 2);
            e.HasOne(x => x.ImportBatch).WithMany(x => x.Needs).HasForeignKey(x => x.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Workplace).WithMany(x => x.CapacityNeeds).HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Year, x.WeekNumber, x.WorkplaceId, x.IsActive });
        });

        modelBuilder.Entity<WeekPlan>(e =>
        {
            e.HasIndex(x => new { x.Year, x.WeekNumber }).IsUnique();
            e.Property(x => x.PublishedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<ShiftAssignment>(e =>
        {
            e.Property(x => x.ShiftName).HasMaxLength(50);
            e.Property(x => x.PlannedHours).HasPrecision(6, 2);
            e.HasOne(x => x.WeekPlan).WithMany(x => x.Assignments).HasForeignKey(x => x.WeekPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Workplace).WithMany(x => x.Assignments).HasForeignKey(x => x.WorkplaceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employee).WithMany(x => x.Assignments).HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.WeekPlanId, x.Date, x.WorkplaceId });
        });

        modelBuilder.Entity<WeekPlanSnapshot>(e =>
        {
            e.HasOne(x => x.WeekPlan).WithMany().HasForeignKey(x => x.WeekPlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.CreatedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.Action).HasMaxLength(50);
        });
    }
}
