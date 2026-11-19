using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Domain.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;
using Services.Models;

namespace Services.Import;

public class ImportService(RepositoryContext db, IAuditService audit) : IImportService
{
    public async Task<ImportPreviewResult> PreviewAsync(Stream stream, string fileName)
    {
        var rows = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ParseCsv(stream)
            : ParseExcel(stream);

        var workplaces = await db.Workplaces.AsNoTracking().ToListAsync();
        var byNumber = workplaces.ToDictionary(w => w.ExternalNumber, StringComparer.OrdinalIgnoreCase);

        var preview = new List<ImportPreviewRow>();
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            string? error = null;
            int? workplaceId = null;

            if (string.IsNullOrWhiteSpace(row.WorkplaceNumber))
                error = "Werkpleknummer ontbreekt.";
            else if (!byNumber.TryGetValue(row.WorkplaceNumber.Trim(), out var wp))
                error = $"Onbekende werkplek '{row.WorkplaceNumber}'.";
            else
                workplaceId = wp.Id;

            if (row.Year < 2000 || row.Year > 2100)
                error ??= "Ongeldig jaar.";
            if (row.WeekNumber is < 1 or > 53)
                error ??= "Ongeldig weeknummer.";
            if (row.RequiredShifts < 0)
                error ??= "Aantal shifts mag niet negatief zijn.";
            if (row.HoursPerShift <= 0)
                error ??= "Uren per shift moet groter dan 0 zijn.";

            var key = $"{row.Year}|{row.WeekNumber}|{row.WorkplaceNumber}|{row.SpecificDate}";
            if (!seen.Add(key))
                error ??= "Dubbele regel in bestand.";

            if (error != null)
                errors.Add($"Rij {row.RowNumber}: {error}");

            preview.Add(row with { WorkplaceId = workplaceId, Error = error });
        }

        return new ImportPreviewResult(
            preview,
            errors,
            preview.Count(r => r.Error is null),
            preview.Count(r => r.Error is not null));
    }

    public async Task<ImportConfirmResult> ConfirmAsync(ImportPreviewResult preview, string fileName, string userName)
    {
        var valid = preview.Rows.Where(r => r.Error is null && r.WorkplaceId.HasValue).ToList();
        if (valid.Count == 0)
            throw new InvalidOperationException("Geen geldige regels om te importeren.");

        var affectedKeys = valid.Select(v => (v.Year, v.WeekNumber, v.WorkplaceId!.Value)).Distinct().ToList();

        var previous = await db.CapacityNeeds
            .Where(n => n.IsActive)
            .Include(n => n.Workplace)
            .AsNoTracking()
            .ToListAsync();

        var previousMap = previous
            .GroupBy(n => (n.Year, n.WeekNumber, n.WorkplaceId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RequiredHours));

        // Deactivate previous needs for affected year/week/workplace
        foreach (var key in affectedKeys)
        {
            var old = await db.CapacityNeeds
                .Where(n => n.IsActive && n.Year == key.Year && n.WeekNumber == key.WeekNumber && n.WorkplaceId == key.Value)
                .ToListAsync();
            foreach (var n in old)
                n.IsActive = false;
        }

        var batch = new ImportBatch
        {
            ImportedAt = DateTime.UtcNow,
            ImportedBy = userName,
            SourceFileName = fileName,
            ProcessedRows = valid.Count,
            ErrorCount = preview.ErrorCount,
            Confirmed = true
        };
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync();

        foreach (var row in valid)
        {
            db.CapacityNeeds.Add(new CapacityNeed
            {
                ImportBatchId = batch.Id,
                Year = row.Year,
                WeekNumber = row.WeekNumber,
                WorkplaceId = row.WorkplaceId!.Value,
                RequiredShifts = row.RequiredShifts,
                HoursPerShift = row.HoursPerShift,
                SpecificDate = row.SpecificDate,
                IsActive = true
            });
        }
        await db.SaveChangesAsync();

        var diffParts = new List<string>();
        foreach (var key in affectedKeys)
        {
            var newHours = valid
                .Where(v => v.Year == key.Year && v.WeekNumber == key.WeekNumber && v.WorkplaceId == key.Value)
                .Sum(v => v.RequiredShifts * v.HoursPerShift);
            previousMap.TryGetValue(key, out var oldHours);
            if (oldHours != newHours)
            {
                var wp = await db.Workplaces.FindAsync(key.Value);
                diffParts.Add($"{wp?.ExternalNumber ?? key.Value.ToString()} W{key.WeekNumber}/{key.Year}: {oldHours:0.##} → {newHours:0.##}u");
            }
        }

        batch.DiffSummary = diffParts.Count == 0
            ? "Geen wijzigingen t.o.v. vorige actieve behoefte."
            : string.Join("; ", diffParts);
        await db.SaveChangesAsync();

        await audit.LogAsync(userName, nameof(ImportBatch), batch.Id, "Import", null, batch.DiffSummary);

        return new ImportConfirmResult(batch.Id, batch.ProcessedRows, batch.ErrorCount, batch.DiffSummary ?? "");
    }

    public async Task<IReadOnlyList<ImportBatch>> GetHistoryAsync() =>
        await db.ImportBatches.OrderByDescending(b => b.ImportedAt).AsNoTracking().ToListAsync();

    private static List<ImportPreviewRow> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line != null) lines.Add(line);
        }
        if (lines.Count == 0) return [];

        var header = SplitCsv(lines[0]);
        var map = BuildHeaderMap(header);
        var rows = new List<ImportPreviewRow>();
        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = SplitCsv(lines[i]);
            rows.Add(MapRow(i + 1, cols, map));
        }
        return rows;
    }

    private static List<ImportPreviewRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var used = sheet.RangeUsed();
        if (used is null) return [];

        var headerRow = used.FirstRow();
        var headers = headerRow.Cells().Select(c => c.GetString()).ToList();
        var map = BuildHeaderMap(headers);
        var rows = new List<ImportPreviewRow>();
        var rowNum = 1;
        foreach (var row in used.RowsUsed().Skip(1))
        {
            rowNum++;
            var cols = row.Cells(1, headers.Count).Select(c => c.GetFormattedString()).ToList();
            rows.Add(MapRow(rowNum, cols, map));
        }
        return rows;
    }

    private static Dictionary<string, int> BuildHeaderMap(IList<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            var h = NormalizeHeader(headers[i]);
            if (!string.IsNullOrEmpty(h))
                map[h] = i;
        }
        return map;
    }

    private static string NormalizeHeader(string header) =>
        header.Trim().ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");

    private static ImportPreviewRow MapRow(int rowNumber, IList<string> cols, Dictionary<string, int> map)
    {
        string Get(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (map.TryGetValue(key, out var idx) && idx < cols.Count)
                    return cols[idx]?.Trim() ?? "";
            }
            return "";
        }

        int.TryParse(Get("jaar", "year"), out var year);
        int.TryParse(Get("weeknummer", "week", "weeknumber"), out var week);
        var wp = Get("werkpleknummer", "werkplek", "workplace", "workplacenumber");
        int.TryParse(Get("aantalbenodigdeshifts", "shifts", "benodigdeshifts", "requiredshifts"), out var shifts);
        var hoursRaw = Get("urenpershift", "uren", "hours", "hoursperhift", "hoursperhift");
        if (!decimal.TryParse(hoursRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var hours) &&
            !decimal.TryParse(hoursRaw, NumberStyles.Any, CultureInfo.GetCultureInfo("nl-NL"), out hours))
            hours = 8;

        DateOnly? date = null;
        var dateRaw = Get("datum", "date", "specificdate");
        if (!string.IsNullOrWhiteSpace(dateRaw) &&
            (DateOnly.TryParse(dateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ||
             DateOnly.TryParse(dateRaw, CultureInfo.GetCultureInfo("nl-NL"), DateTimeStyles.None, out d)))
            date = d;

        if (year == 0) year = DateTime.Today.Year;

        return new ImportPreviewRow(rowNumber, year, week, wp, null, shifts, hours, date, null);
    }

    private static List<string> SplitCsv(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if ((c == ';' || c == ',') && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }
            sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }
}
