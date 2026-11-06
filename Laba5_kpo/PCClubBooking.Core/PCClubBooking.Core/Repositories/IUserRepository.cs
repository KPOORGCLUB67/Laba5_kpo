
using PCClubBooking.Core.Models;

namespace PCClubBooking.Core.Repositories;

public interface IUserRepository
{
    User? FindByLogin(string login);

    User? FindById(int id);

    int Add(User user);

    IEnumerable<User> GetAll();

    void Update(User user);
}
