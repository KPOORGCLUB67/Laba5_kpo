
using System.Text;

namespace PCClubBooking.Core.Services;

public class FileExportService
{
    private readonly string _exportDirectory;

    public FileExportService(string exportDirectory)
    {
        if (string.IsNullOrWhiteSpace(exportDirectory))
            throw new ArgumentException(
                "Не указана папка экспорта.");

        _exportDirectory = Path.GetFullPath(exportDirectory);
    }

    public string CreateCsvFile(
        string csvData,
        string fileName)
    {
        if (csvData == null)
            throw new ArgumentNullException(nameof(csvData));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                "Не указано имя файла.");

        Directory.CreateDirectory(_exportDirectory);

        var safeName = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "RevenueReport";

        var fullPath = Path.Combine(
            _exportDirectory,
            safeName + ".csv"
        );

        File.WriteAllText(
            fullPath,
            csvData,
            new UTF8Encoding(true)
        );

        return fullPath;
    }
}
