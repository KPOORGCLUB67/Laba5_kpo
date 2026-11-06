
namespace PCClubBooking.Core.Services;

public class ReportController
{
    private readonly ReportService _reportService;

    private readonly FileExportService _fileExportService;

    public ReportController(
        ReportService reportService,
        FileExportService fileExportService)
    {
        _reportService = reportService;
        _fileExportService = fileExportService;
    }

    public (string FilePath, decimal TotalRevenue)
        GenerateAndExportRevenueReport(
            DateTime from,
            DateTime to)
    {
        var csvData = _reportService.BuildRevenueCsv(
            from,
            to
        );

        var totalRevenue = _reportService.TotalRevenue(
            from,
            to
        );

        var fileName =
            $"RevenueReport_{DateTime.Now:yyyyMMdd_HHmmss}";

        var filePath = _fileExportService.CreateCsvFile(
            csvData,
            fileName
        );

        return (filePath, totalRevenue);
    }
}
