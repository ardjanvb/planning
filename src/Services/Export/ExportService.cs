using ClosedXML.Excel;
using Domain.Contexts;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;
using Services.Logic;

namespace Services.Export;

public class ExportService(RepositoryContext db, IPlanningService planning) : IExportService
{
    public async Task<byte[]> ExportWorkplacePlanningAsync(int year, int weekNumber)
    {
        var overview = await planning.GetOverviewAsync(year, weekNumber);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Week {weekNumber}");
        ws.Cell(1, 1).Value = $"Planning per werkplek – {year} week {weekNumber}";
        ws.Cell(1, 1).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = "Werkplek";
        var col = 2;
        foreach (var day in overview.Days)
        {
            ws.Cell(3, col).Value = $"{WeekHelper.DayLabel(day)} {day:dd-MM}";
            col++;
        }
        ws.Cell(3, col).Value = "Benodigd";
        ws.Cell(3, col + 1).Value = "Gepland";
        ws.Range(3, 1, 3, col + 1).Style.Font.Bold = true;

        var row = 4;
        foreach (var wp in overview.ByWorkplace)
        {
            ws.Cell(row, 1).Value = $"{wp.ExternalNumber} {wp.WorkplaceName}";
            col = 2;
            foreach (var day in overview.Days)
            {
                var cell = wp.Days[day];
                ws.Cell(row, col).Value = string.Join(", ", cell.Assignments.Select(a =>
                    a.EmployeeName is null ? $"({a.ShiftName})" : a.EmployeeName));
                col++;
            }
            ws.Cell(row, col).Value = (double)wp.RequiredHours;
            ws.Cell(row, col + 1).Value = (double)wp.PlannedHours;
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportEmployeePlanningAsync(int year, int weekNumber)
    {
        var overview = await planning.GetOverviewAsync(year, weekNumber);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Medewerkers W{weekNumber}");
        ws.Cell(1, 1).Value = $"Planning per medewerker – {year} week {weekNumber}";
        ws.Cell(1, 1).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = "Medewerker";
        var col = 2;
        foreach (var day in overview.Days)
        {
            ws.Cell(3, col).Value = $"{WeekHelper.DayLabel(day)} {day:dd-MM}";
            col++;
        }
        ws.Cell(3, col).Value = "Contract";
        ws.Cell(3, col + 1).Value = "Gepland";
        ws.Range(3, 1, 3, col + 1).Style.Font.Bold = true;

        var row = 4;
        foreach (var emp in overview.ByEmployee)
        {
            ws.Cell(row, 1).Value = emp.EmployeeName;
            col = 2;
            foreach (var day in overview.Days)
            {
                var cell = emp.Days[day];
                if (cell.Assignments.Count == 0)
                    ws.Cell(row, col).Value = emp.HasApprovedAbsence ? "Vrij" : "";
                else
                    ws.Cell(row, col).Value = string.Join(", ", cell.Assignments.Select(a => $"{a.WorkplaceName} {a.StartTime:HH\\:mm}-{a.EndTime:HH\\:mm}"));
                col++;
            }
            ws.Cell(row, col).Value = (double)emp.ContractHours;
            ws.Cell(row, col + 1).Value = (double)emp.PlannedHours;
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportCapacityAsync(int year, int weekNumber)
    {
        var overview = await planning.GetOverviewAsync(year, weekNumber);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Capaciteit");
        ws.Cell(1, 1).Value = $"Benodigd vs gepland – {year} week {weekNumber}";
        ws.Cell(1, 1).Style.Font.Bold = true;

        var headers = new[] { "Werkplek", "Nummer", "Afdeling", "Benodigd shifts", "Benodigd uren", "Gepland shifts", "Gepland uren", "Verschil" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(3, i + 1).Value = headers[i];
        ws.Range(3, 1, 3, headers.Length).Style.Font.Bold = true;

        var row = 4;
        foreach (var c in overview.Capacity)
        {
            ws.Cell(row, 1).Value = c.WorkplaceName;
            ws.Cell(row, 2).Value = c.ExternalNumber;
            ws.Cell(row, 3).Value = c.DepartmentName;
            ws.Cell(row, 4).Value = c.RequiredShifts;
            ws.Cell(row, 5).Value = (double)c.RequiredHours;
            ws.Cell(row, 6).Value = c.PlannedShifts;
            ws.Cell(row, 7).Value = (double)c.PlannedHours;
            ws.Cell(row, 8).Value = (double)c.DifferenceHours;
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public async Task<byte[]> ExportEmployeesAsync()
    {
        var employees = await db.Employees
            .Include(e => e.PrimaryJobFunction)
            .Include(e => e.PrimaryCrew)
            .Include(e => e.Qualifications).ThenInclude(q => q.Qualification)
            .OrderBy(e => e.LastName)
            .AsNoTracking()
            .ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Medewerkers");
        var headers = new[] { "Personeelsnr", "Naam", "Functie", "Ploeg", "Contracturen", "FTE", "BHV", "BHV tot", "Kwalificaties", "Actief" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;

        var row = 2;
        foreach (var e in employees)
        {
            ws.Cell(row, 1).Value = e.PersonnelNumber;
            ws.Cell(row, 2).Value = e.DisplayName;
            ws.Cell(row, 3).Value = e.PrimaryJobFunction?.Name;
            ws.Cell(row, 4).Value = e.PrimaryCrew?.Name;
            ws.Cell(row, 5).Value = (double)e.ContractHoursPerWeek;
            ws.Cell(row, 6).Value = (double)e.Fte;
            ws.Cell(row, 7).Value = e.IsBhv ? "Ja" : "Nee";
            ws.Cell(row, 8).Value = e.BhvValidUntil?.ToString("dd-MM-yyyy");
            ws.Cell(row, 9).Value = string.Join(", ", e.Qualifications.Select(q => q.Qualification?.Name));
            ws.Cell(row, 10).Value = e.Active ? "Ja" : "Nee";
            row++;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
