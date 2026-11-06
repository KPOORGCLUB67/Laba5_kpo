using Moq;
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;
using PCClubBooking.Core.Services;
//хуй
//dsadasdsada
//пидорки6767
//секс
//членыы
namespace PCCClubBooking.Tests
{
    [TestClass]
    public class BookingServiceTests
    {
        // Добавляем = null!; чтобы убрать предупреждение о NULL
        private Mock<IBookingRepository> _mockBookings = null!;
        private Mock<IPcRepository> _mockPcs = null!;
        private TariffService _tariffs = null!;
        private BookingService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockBookings = new Mock<IBookingRepository>();
            _mockPcs = new Mock<IPcRepository>();

            _tariffs = new TariffService(new List<Tariff>
            {
                new Tariff { Category = PcCategory.Standard, PricePerHour = 100, DiscountPercent = 0 }
            });

            _service = new BookingService(_mockBookings.Object, _mockPcs.Object, _tariffs);
        }


        [TestMethod]
        public void Create_ValidData_ReturnsBookingAndUpdatesPcStatus()
        {
            // ARRANGE
            int userId = 1;
            int pcId = 10;
            DateTime start = DateTime.Now.AddHours(2);
            DateTime end = start.AddHours(2);

            var pc = new Pc { Id = pcId, Category = PcCategory.Standard, Status = PcStatus.Free };

            _mockPcs.Setup(r => r.FindById(pcId)).Returns(pc);
            _mockBookings.Setup(r => r.IsIntervalBusy(pcId, start, end)).Returns(false);
            _mockBookings.Setup(r => r.Add(It.IsAny<Booking>())).Returns(100);

            // ACT
            var result = _service.Create(userId, pcId, start, end);

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual(100, result.Id);
            Assert.AreEqual(200, result.TotalPrice); // 2 часа * 100
            Assert.AreEqual(PcStatus.Booked, pc.Status);

            _mockBookings.Verify(r => r.Add(It.IsAny<Booking>()), Times.Once);
            _mockPcs.Verify(r => r.Update(pc), Times.Once);
        }


        [TestMethod]
        public void Create_PcIsBroken_ThrowsInvalidOperationException()
        {
            // ARRANGE
            int pcId = 10;
            var brokenPc = new Pc { Id = pcId, Status = PcStatus.Broken };
            _mockPcs.Setup(r => r.FindById(pcId)).Returns(brokenPc);

            // ACT & ASSERT
            try
            {
                _service.Create(1, pcId, DateTime.Now.AddHours(1), DateTime.Now.AddHours(2));
                Assert.Fail("Ожидалось исключение InvalidOperationException, но его не было.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("ПК находится на ремонте.", ex.Message);
            }
        }


        [TestMethod]
        public void Create_IntervalIsBusy_ThrowsInvalidOperationException()
        {
            // ARRANGE
            int pcId = 10;
            var pc = new Pc { Id = pcId, Status = PcStatus.Free };
            _mockPcs.Setup(r => r.FindById(pcId)).Returns(pc);

            _mockBookings.Setup(r => r.IsIntervalBusy(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .Returns(true);

            // ACT & ASSERT
            try
            {
                _service.Create(1, pcId, DateTime.Now.AddHours(1), DateTime.Now.AddHours(2));
                Assert.Fail("Ожидалось исключение InvalidOperationException, но его не было.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Выбранный интервал уже занят.", ex.Message);
            }
        }


        [TestMethod]
        public void Cancel_ValidBooking_ChangesStatusToCancelled()
        {
            // ARRANGE
            int userId = 1;
            int pcId = 10;
            int bookingId = 5;

            var booking = new Booking
            {
                Id = bookingId,
                UserId = userId,
                PcId = pcId,
                Start = DateTime.Now.AddHours(2),
                IsCancelled = false
            };
            var pc = new Pc { Id = pcId, Status = PcStatus.Booked };

            _mockBookings.Setup(r => r.GetActive()).Returns(new List<Booking> { booking });
            _mockPcs.Setup(r => r.FindById(pcId)).Returns(pc);

            // ACT
            _service.Cancel(bookingId, userId);

            // ASSERT
            Assert.IsTrue(booking.IsCancelled);
            Assert.AreEqual(PcStatus.Free, pc.Status);
            _mockBookings.Verify(r => r.Update(booking), Times.Once);
            _mockPcs.Verify(r => r.Update(pc), Times.Once);
        }

        [TestMethod]
        public void Cancel_AnotherUsersBooking_ThrowsUnauthorizedAccessException()
        {
            // ARRANGE
            int bookingId = 5;
            var booking = new Booking { Id = bookingId, UserId = 2, Start = DateTime.Now.AddHours(2) };

            _mockBookings.Setup(r => r.GetActive()).Returns(new List<Booking> { booking });

            // ACT & ASSERT
            try
            {
                _service.Cancel(bookingId, 1);
                Assert.Fail("Ожидалось исключение UnauthorizedAccessException, но его не было.");
            }
            catch (UnauthorizedAccessException ex)
            {
                Assert.AreEqual("Нельзя отменить чужое бронирование.", ex.Message);
            }
        }


        [TestMethod]
        public void Cancel_TooLate_ThrowsInvalidOperationException()
        {
            // ARRANGE
            int bookingId = 5;
            int userId = 1;
            var booking = new Booking { Id = bookingId, UserId = userId, Start = DateTime.Now.AddMinutes(30) };

            _mockBookings.Setup(r => r.GetActive()).Returns(new List<Booking> { booking });

            // ACT & ASSERT
            try
            {
                _service.Cancel(bookingId, userId);
                Assert.Fail("Ожидалось исключение InvalidOperationException, но его не было.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("Отмена возможна не позднее чем за 1 час до начала.", ex.Message);
            }
        }
    }
}

