
using PCClubBooking.Core.Models;

namespace PCClubBooking.Core.Repositories;

public interface IServiceRepository
{
    IEnumerable<Service> GetAll();

    int Add(Service service);
}
