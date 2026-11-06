using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;
using PCClubBooking.Core.Services;

namespace PCClubBooking.Tests;

[TestClass]
public class BookingServiceTests
{
    private Mock<IBookingRepository> _mockBookings = null!;
    private Mock<IPcRepository> _mockPcs = null!;
    private TariffService _tariffs = null!;
    private BookingService _service = null!;

    // Подготавливаем независимое тестовое окружение перед каждым тестом.
    [TestInitialize]
    public void Setup()
    {
        _mockBookings = new Mock<IBookingRepository>();
        _mockPcs = new Mock<IPcRepository>();

        // Используем настоящий TariffService.
        _tariffs = new TariffService(new List<Tariff>
        {
            new Tariff
            {
                Category = PcCategory.Standard,
                PricePerHour = 100m,
                DiscountPercent = 0m
            }
        });

        _service = new BookingService(
            _mockBookings.Object,
            _mockPcs.Object,
            _tariffs);

        // По умолчанию временной интервал свободен.
        _mockBookings
            .Setup(r => r.IsIntervalBusy(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .Returns(false);
    }

    // Создание тестового компьютера.
    private static Pc CreatePc(
        PcStatus status = PcStatus.Free,
        PcCategory category = PcCategory.Standard)
    {
        return new Pc
        {
            Id = 10,
            Category = category,
            Status = status
        };
    }

    // Настройка поиска компьютера через Mock.
    private void SetupPc(Pc pc)
    {
        _mockPcs.Setup(r => r.FindById(pc.Id)).Returns(pc);
    }

    // Проверка ожидаемого исключения.
    // Используется вместо Assert.ThrowsException,
    // которого нет в установленной версии MSTest.
    private static TException AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail(
            $"Ожидалось исключение {typeof(TException).Name}, но его не было.");

        return null!;
    }

    // ТЕСТЫ МЕТОДА CREATE()

    // ТЕСТ 1. Успешное создание бронирования.
    [TestMethod]
    public void Create_ValidData_ReturnsBookingAndUpdatesPcStatus()
    {
        // ARRANGE
        const int userId = 1;
        const int pcId = 10;

        var start = DateTime.Now.AddHours(2);
        var end = start.AddHours(2);

        var pc = CreatePc();

        SetupPc(pc);

        _mockBookings
            .Setup(r => r.Add(It.IsAny<Booking>()))
            .Returns(100);

        // ACT
        var result = _service.Create(
            userId,
            pcId,
            start,
            end);

        // ASSERT
        Assert.IsNotNull(result);
        Assert.AreEqual(100, result.Id);
        Assert.AreEqual(userId, result.UserId);
        Assert.AreEqual(pcId, result.PcId);
        Assert.AreEqual(200m, result.TotalPrice);
        Assert.IsFalse(result.IsCancelled);
        Assert.AreEqual(PcStatus.Booked, pc.Status);

        _mockBookings.Verify(
            r => r.Add(It.IsAny<Booking>()),
            Times.Once);

        _mockPcs.Verify(
            r => r.Update(pc),
            Times.Once);
    }

    // ТЕСТ 2. Попытка забронировать сломанный компьютер.
    [TestMethod]
    public void Create_PcIsBroken_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var pc = CreatePc(PcStatus.Broken);

        SetupPc(pc);

        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<InvalidOperationException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddHours(1)));

        // ASSERT
        Assert.AreEqual(
            "ПК находится на ремонте.",
            exception.Message);

        _mockBookings.Verify(
            r => r.Add(It.IsAny<Booking>()),
            Times.Never);

        _mockPcs.Verify(
            r => r.Update(It.IsAny<Pc>()),
            Times.Never);
    }

    // ТЕСТ 3. Попытка забронировать занятый интервал.
    [TestMethod]
    public void Create_IntervalIsBusy_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        _mockBookings
            .Setup(r => r.IsIntervalBusy(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .Returns(true);

        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<InvalidOperationException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddHours(1)));

        // ASSERT
        Assert.AreEqual(
            "Выбранный интервал уже занят.",
            exception.Message);

        _mockBookings.Verify(
            r => r.Add(It.IsAny<Booking>()),
            Times.Never);

        _mockPcs.Verify(
            r => r.Update(It.IsAny<Pc>()),
            Times.Never);
    }

    // ТЕСТ 4. Передан некорректный ID пользователя.
    [TestMethod]
    public void Create_InvalidUserId_ThrowsArgumentException()
    {
        // ARRANGE
        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                0,
                10,
                start,
                start.AddHours(1)));

        // ASSERT
        Assert.AreEqual(
            "Не указан пользователь.",
            exception.Message);

        _mockPcs.Verify(
            r => r.FindById(It.IsAny<int>()),
            Times.Never);
    }

    // ТЕСТ 5. Компьютер с указанным ID не найден.
    [TestMethod]
    public void Create_PcNotFound_ThrowsArgumentException()
    {
        // ARRANGE
        _mockPcs
            .Setup(r => r.FindById(10))
            .Returns((Pc?)null);

        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                1,
                10,
                start,
                start.AddHours(1)));

        // ASSERT
        Assert.AreEqual(
            "ПК не найден.",
            exception.Message);
    }

    // ТЕСТ 6. Время окончания раньше времени начала.
    [TestMethod]
    public void Create_EndBeforeStart_ThrowsArgumentException()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        var start = DateTime.Now.AddHours(2);
        var end = start.AddMinutes(-10);

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                end));

        // ASSERT
        Assert.AreEqual(
            "Окончание должно быть позже начала.",
            exception.Message);
    }

    // ТЕСТ 7. Продолжительность бронирования меньше 30 минут.
    [TestMethod]
    public void Create_DurationBelowMinimum_ThrowsArgumentException()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddMinutes(29)));

        // ASSERT
        Assert.AreEqual(
            "Длительность бронирования должна быть от 30 минут до 24 часов.",
            exception.Message);
    }

    // ТЕСТ 8. Продолжительность бронирования больше 24 часов.
    [TestMethod]
    public void Create_DurationAboveMaximum_ThrowsArgumentException()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddHours(25)));

        // ASSERT
        Assert.AreEqual(
            "Длительность бронирования должна быть от 30 минут до 24 часов.",
            exception.Message);
    }

    // ТЕСТ 9. Попытка забронировать время в прошлом.
    [TestMethod]
    public void Create_StartInPast_ThrowsArgumentException()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        var start = DateTime.Now.AddHours(-2);

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddHours(1)));

        // ASSERT
        Assert.AreEqual(
            "Нельзя бронировать время в прошлом.",
            exception.Message);
    }

    // ТЕСТ 10. Передана услуга с отрицательной стоимостью.
    [TestMethod]
    public void Create_NegativeServicePrice_ThrowsArgumentException()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        var start = DateTime.Now.AddHours(2);

        var services = new[]
        {
            new Service
            {
                Name = "Отрицательная услуга",
                Price = -10m
            }
        };

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddHours(1),
                services));

        // ASSERT
        Assert.AreEqual(
            "Стоимость услуги не может быть отрицательной.",
            exception.Message);

        _mockBookings.Verify(
            r => r.Add(It.IsAny<Booking>()),
            Times.Never);
    }

    // ТЕСТ 11. Стоимость дополнительной услуги включается в цену бронирования.
    [TestMethod]
    public void Create_ExtraServicePriceIsIncludedInTotalPrice()
    {
        // ARRANGE
        var pc = CreatePc();

        SetupPc(pc);

        _mockBookings
            .Setup(r => r.Add(It.IsAny<Booking>()))
            .Returns(101);

        var start = DateTime.Now.AddHours(2);

        var services = new[]
        {
            new Service
            {
                Name = "Напиток",
                Price = 40m
            }
        };

        // ACT
        var result = _service.Create(
            1,
            pc.Id,
            start,
            start.AddHours(2),
            services);

        // ASSERT
        Assert.AreEqual(240m, result.TotalPrice);

        Assert.AreEqual(
            PcStatus.Booked,
            pc.Status);

        _mockBookings.Verify(
            r => r.Add(It.IsAny<Booking>()),
            Times.Once);
    }

    // ТЕСТ 12. Для категории компьютера не задан тариф.
    [TestMethod]
    public void Create_TariffIsMissing_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var pc = CreatePc(category: PcCategory.Gaming);

        SetupPc(pc);

        var start = DateTime.Now.AddHours(2);

        // ACT
        var exception = AssertThrows<InvalidOperationException>(
            () => _service.Create(
                1,
                pc.Id,
                start,
                start.AddHours(1)));

        // ASSERT
        Assert.AreEqual(
            "Тариф для Gaming не задан",
            exception.Message);

        _mockBookings.Verify(
            r => r.Add(It.IsAny<Booking>()),
            Times.Never);
    }

    // ТЕСТЫ МЕТОДА CANCEL()

    // ТЕСТ 13. Успешная отмена бронирования.
    [TestMethod]
    public void Cancel_ValidBooking_ChangesStatusToCancelledAndFreesPc()
    {
        // ARRANGE
        const int userId = 1;
        const int pcId = 10;
        const int bookingId = 5;

        var booking = new Booking
        {
            Id = bookingId,
            UserId = userId,
            PcId = pcId,
            Start = DateTime.Now.AddHours(2),
            IsCancelled = false
        };

        var pc = CreatePc(PcStatus.Booked);

        SetupPc(pc);

        // Возвращаем только активные бронирования.
        var bookings = new List<Booking> { booking };

        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(() => bookings
                .Where(b => !b.IsCancelled)
                .ToList());

        // ACT
        _service.Cancel(bookingId, userId);

        // ASSERT
        Assert.IsTrue(booking.IsCancelled);

        Assert.AreEqual(
            PcStatus.Free,
            pc.Status);

        _mockBookings.Verify(
            r => r.Update(booking),
            Times.Once);

        _mockPcs.Verify(
            r => r.Update(pc),
            Times.Once);
    }

    // ТЕСТ 14. Пользователь пытается отменить чужое бронирование.
    [TestMethod]
    public void Cancel_AnotherUsersBooking_ThrowsUnauthorizedAccessException()
    {
        // ARRANGE
        const int bookingId = 5;

        var booking = new Booking
        {
            Id = bookingId,
            UserId = 2,
            PcId = 10,
            Start = DateTime.Now.AddHours(2)
        };

        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(new List<Booking> { booking });

        // ACT
        var exception = AssertThrows<UnauthorizedAccessException>(
            () => _service.Cancel(bookingId, 1));

        // ASSERT
        Assert.AreEqual(
            "Нельзя отменить чужое бронирование.",
            exception.Message);

        _mockBookings.Verify(
            r => r.Update(It.IsAny<Booking>()),
            Times.Never);
    }

    // ТЕСТ 15. Попытка отменить бронирование менее чем за час.
    [TestMethod]
    public void Cancel_TooLate_ThrowsInvalidOperationException()
    {
        // ARRANGE
        const int bookingId = 5;
        const int userId = 1;

        var booking = new Booking
        {
            Id = bookingId,
            UserId = userId,
            PcId = 10,
            Start = DateTime.Now.AddMinutes(30)
        };

        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(new List<Booking> { booking });

        // ACT
        var exception = AssertThrows<InvalidOperationException>(
            () => _service.Cancel(bookingId, userId));

        // ASSERT
        Assert.AreEqual(
            "Отмена возможна не позднее чем за 1 час до начала.",
            exception.Message);

        _mockBookings.Verify(
            r => r.Update(It.IsAny<Booking>()),
            Times.Never);
    }

    // ТЕСТ 16. Бронирование с указанным ID не найдено.
    [TestMethod]
    public void Cancel_BookingNotFound_ThrowsArgumentException()
    {
        // ARRANGE
        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(Array.Empty<Booking>());

        // ACT
        var exception = AssertThrows<ArgumentException>(
            () => _service.Cancel(999, 1));

        // ASSERT
        Assert.AreEqual(
            "Активное бронирование не найдено.",
            exception.Message);
    }

    // ТЕСТ 17. После отмены одного бронирования другое остаётся активным.
    [TestMethod]
    public void Cancel_WhenAnotherActiveBookingExists_PcRemainsBooked()
    {
        // ARRANGE
        const int userId = 1;
        const int pcId = 10;

        var bookingToCancel = new Booking
        {
            Id = 5,
            UserId = userId,
            PcId = pcId,
            Start = DateTime.Now.AddHours(2)
        };

        var anotherBooking = new Booking
        {
            Id = 6,
            UserId = 2,
            PcId = pcId,
            Start = DateTime.Now.AddHours(4)
        };

        var pc = CreatePc(PcStatus.Booked);

        SetupPc(pc);

        var bookings = new List<Booking>
        {
            bookingToCancel,
            anotherBooking
        };

        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(() => bookings
                .Where(b => !b.IsCancelled)
                .ToList());

        // ACT
        _service.Cancel(bookingToCancel.Id, userId);

        // ASSERT
        Assert.IsTrue(bookingToCancel.IsCancelled);

        Assert.IsFalse(anotherBooking.IsCancelled);

        Assert.AreEqual(
            PcStatus.Booked,
            pc.Status);

        _mockPcs.Verify(
            r => r.Update(pc),
            Times.Once);
    }

    // ТЕСТ 18. Отмена бронирования не изменяет статус сломанного ПК.
    [TestMethod]
    public void Cancel_WhenPcIsBroken_DoesNotChangeBrokenStatus()
    {
        // ARRANGE
        const int userId = 1;
        const int pcId = 10;

        var booking = new Booking
        {
            Id = 5,
            UserId = userId,
            PcId = pcId,
            Start = DateTime.Now.AddHours(2)
        };

        var pc = CreatePc(PcStatus.Broken);

        SetupPc(pc);

        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(new List<Booking> { booking });

        // ACT
        _service.Cancel(booking.Id, userId);

        // ASSERT
        Assert.IsTrue(booking.IsCancelled);

        Assert.AreEqual(
            PcStatus.Broken,
            pc.Status);

        _mockBookings.Verify(
            r => r.Update(booking),
            Times.Once);

        _mockPcs.Verify(
            r => r.Update(It.IsAny<Pc>()),
            Times.Never);
    }

    // ТЕСТ 19. При отсутствии компьютера его статус не обновляется.
    [TestMethod]
    public void Cancel_WhenPcDoesNotExist_DoesNotUpdatePc()
    {
        // ARRANGE
        const int userId = 1;
        const int pcId = 10;

        var booking = new Booking
        {
            Id = 5,
            UserId = userId,
            PcId = pcId,
            Start = DateTime.Now.AddHours(2)
        };

        _mockBookings
            .Setup(r => r.GetActive())
            .Returns(new List<Booking> { booking });

        _mockPcs
            .Setup(r => r.FindById(pcId))
            .Returns((Pc?)null);

        // ACT
        _service.Cancel(booking.Id, userId);

        // ASSERT
        Assert.IsTrue(booking.IsCancelled);

        _mockBookings.Verify(
            r => r.Update(booking),
            Times.Once);

        _mockPcs.Verify(
            r => r.Update(It.IsAny<Pc>()),
            Times.Never);
    }
}