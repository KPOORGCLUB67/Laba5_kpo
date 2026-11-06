
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;

namespace PCClubBooking.Core.Services;

public class BookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IPcRepository _pcs;
    private readonly TariffService _tariffs;

    public const double MinHours = 0.5;

    public const double MaxHours = 24.0;

    public static readonly TimeSpan CancelBeforeStart =
        TimeSpan.FromHours(1);

    public BookingService(
        IBookingRepository bookings,
        IPcRepository pcs,
        TariffService tariffs)
    {
        _bookings = bookings ??
            throw new ArgumentNullException(nameof(bookings));

        _pcs = pcs ??
            throw new ArgumentNullException(nameof(pcs));

        _tariffs = tariffs ??
            throw new ArgumentNullException(nameof(tariffs));
    }

    public Booking Create(
        int userId,
        int pcId,
        DateTime start,
        DateTime end,
        IEnumerable<Service>? extraServices = null)
    {
        if (userId <= 0)
            throw new ArgumentException(
                "Не указан пользователь.");

        var pc = _pcs.FindById(pcId)
            ?? throw new ArgumentException("ПК не найден.");

        if (pc.Status == PcStatus.Broken)
            throw new InvalidOperationException(
                "ПК находится на ремонте.");

        if (end <= start)
            throw new ArgumentException(
                "Окончание должно быть позже начала.");

        var hours = (end - start).TotalHours;

        if (hours < MinHours || hours > MaxHours)
            throw new ArgumentException(
                "Длительность бронирования должна быть от 30 минут до 24 часов.");

        if (start < DateTime.Now)
            throw new ArgumentException(
                "Нельзя бронировать время в прошлом.");

        if (_bookings.IsIntervalBusy(pcId, start, end))
            throw new InvalidOperationException(
                "Выбранный интервал уже занят.");

        var services = extraServices?.ToList()
            ?? new List<Service>();

        if (services.Any(s => s.Price < 0))
            throw new ArgumentException(
                "Стоимость услуги не может быть отрицательной.");

        var price =
            _tariffs.Calculate(pc.Category, hours)
            + services.Sum(s => s.Price);

        var booking = new Booking
        {
            UserId = userId,
            PcId = pcId,
            Start = start,
            End = end,
            TotalPrice = price,
            IsCancelled = false
        };

        booking.Id = _bookings.Add(booking);

        pc.Status = PcStatus.Booked;

        _pcs.Update(pc);

        return booking;
    }

    public void Cancel(int bookingId, int userId)
    {
        var booking = _bookings.GetActive()
            .FirstOrDefault(b => b.Id == bookingId)
            ?? throw new ArgumentException(
                "Активное бронирование не найдено.");

        if (booking.UserId != userId)
            throw new UnauthorizedAccessException(
                "Нельзя отменить чужое бронирование.");

        if (booking.Start - DateTime.Now < CancelBeforeStart)
            throw new InvalidOperationException(
                "Отмена возможна не позднее чем за 1 час до начала.");

        booking.IsCancelled = true;

        _bookings.Update(booking);

        // Пересчитываем статус компьютера после отмены.

        var pc = _pcs.FindById(booking.PcId);

        if (pc != null && pc.Status != PcStatus.Broken)
        {
            pc.Status = _bookings.GetActive()
                .Any(b => b.PcId == booking.PcId)
                ? PcStatus.Booked
                : PcStatus.Free;

            _pcs.Update(pc);
        }
    }
}
