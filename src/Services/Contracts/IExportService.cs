namespace Services.Contracts;

public interface IExportService
{
    Task<byte[]> ExportWorkplacePlanningAsync(int year, int weekNumber);
    Task<byte[]> ExportEmployeePlanningAsync(int year, int weekNumber);
    Task<byte[]> ExportCapacityAsync(int year, int weekNumber);
    Task<byte[]> ExportEmployeesAsync();
}
