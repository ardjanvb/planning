using Services.Models;

namespace Services.Contracts;

public interface IImportService
{
    Task<ImportPreviewResult> PreviewAsync(Stream stream, string fileName);
    Task<ImportConfirmResult> ConfirmAsync(ImportPreviewResult preview, string fileName, string userName);
    Task<IReadOnlyList<Domain.Entities.ImportBatch>> GetHistoryAsync();
}
