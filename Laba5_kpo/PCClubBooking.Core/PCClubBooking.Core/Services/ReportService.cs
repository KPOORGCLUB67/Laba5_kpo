
using System.Text;
using PCClubBooking.Core.Repositories;

namespace PCClubBooking.Core.Services;

public class ReportService
{
    private readonly IBookingRepository _bookings;

    public ReportService(IBookingRepository bookings)
    {
        _bookings = bookings ??
            throw new ArgumentNullException(nameof(bookings));
    }

    public string BuildRevenueCsv(DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentException(
                "Начало периода не может быть позже окончания.");

        var data = _bookings.GetActive()
            .Where(b =>
                !b.IsCancelled &&
                b.Start >= from &&
                b.Start <= to)
            .OrderBy(b => b.Start);

        var sb = new StringBuilder();

        sb.AppendLine(
            "Id;PcId;UserId;Start;End;Hours;TotalPrice");

        foreach (var booking in data)
        {
            sb.AppendLine(
                $"{booking.Id};{booking.PcId};{booking.UserId};" +
                $"{booking.Start:yyyy-MM-dd HH:mm};" +
                $"{booking.End:yyyy-MM-dd HH:mm};" +
                $"{booking.DurationHours:F2};" +
                $"{booking.TotalPrice:F2}");
        }

        return sb.ToString();
    }

    public decimal TotalRevenue(DateTime from, DateTime to)
    {
        if (from > to)
            throw new ArgumentException(
                "Начало периода не может быть позже окончания.");

        return _bookings.GetActive()
            .Where(b =>
                !b.IsCancelled &&
                b.Start >= from &&
                b.Start <= to)
            .Sum(b => b.TotalPrice);
    }
}
