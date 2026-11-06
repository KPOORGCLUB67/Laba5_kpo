
using PCClubBooking.Core.Models;

namespace PCClubBooking.Core.Repositories;

public interface IPcRepository
{
    Pc? FindById(int id);

    IEnumerable<Pc> GetAll();

    int Add(Pc pc);

    void Update(Pc pc);

    void Delete(int id);
}
