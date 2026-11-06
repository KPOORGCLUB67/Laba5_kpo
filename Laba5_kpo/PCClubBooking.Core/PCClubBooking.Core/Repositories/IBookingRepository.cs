
using PCClubBooking.Core.Models;

namespace PCClubBooking.Core.Repositories;

public interface IBookingRepository
{
    IEnumerable<Booking> GetByUser(int userId);

    IEnumerable<Booking> GetActive();

    int Add(Booking booking);

    void Update(Booking booking);

    bool IsIntervalBusy(
        int pcId,
        DateTime start,
        DateTime end
    );
}
