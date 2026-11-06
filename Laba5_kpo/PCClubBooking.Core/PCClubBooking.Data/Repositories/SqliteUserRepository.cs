
using Microsoft.Data.Sqlite;
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;

namespace PCClubBooking.Data.Repositories;

public class SqliteUserRepository : IUserRepository
{
    private readonly Database _database;

    public SqliteUserRepository(Database database)
    {
        _database = database;
    }

    public User? FindByLogin(string login)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Login, PasswordHash, Name,
                   Phone, Role, IsBlocked
            FROM Users
            WHERE Login = $login
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$login", login);

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadUser(reader) : null;
    }

    public User? FindById(int id)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Login, PasswordHash, Name,
                   Phone, Role, IsBlocked
            FROM Users
            WHERE Id = $id
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadUser(reader) : null;
    }

    public int Add(User user)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Users(
                Login, PasswordHash, Name, Phone, Role, IsBlocked
            )
            VALUES(
                $login, $hash, $name, $phone, $role, $blocked
            );

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$login", user.Login);

        command.Parameters.AddWithValue("$hash", user.PasswordHash);

        command.Parameters.AddWithValue("$name", user.Name);

        command.Parameters.AddWithValue("$phone", user.Phone);

        command.Parameters.AddWithValue("$role", (int)user.Role);

        command.Parameters.AddWithValue(
            "$blocked",
            user.IsBlocked ? 1 : 0
        );

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public IEnumerable<User> GetAll()
    {
        var result = new List<User>();

        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Login, PasswordHash, Name,
                   Phone, Role, IsBlocked
            FROM Users
            ORDER BY Id;
            """;

        using var reader = command.ExecuteReader();

        while (reader.Read())
            result.Add(ReadUser(reader));

        return result;
    }

    public void Update(User user)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Users
            SET Login=$login,
                PasswordHash=$hash,
                Name=$name,
                Phone=$phone,
                Role=$role,
                IsBlocked=$blocked
            WHERE Id=$id;
            """;

        command.Parameters.AddWithValue("$login", user.Login);

        command.Parameters.AddWithValue("$hash", user.PasswordHash);

        command.Parameters.AddWithValue("$name", user.Name);

        command.Parameters.AddWithValue("$phone", user.Phone);

        command.Parameters.AddWithValue("$role", (int)user.Role);

        command.Parameters.AddWithValue(
            "$blocked",
            user.IsBlocked ? 1 : 0
        );

        command.Parameters.AddWithValue("$id", user.Id);

        command.ExecuteNonQuery();
    }

    private static User ReadUser(SqliteDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(0),
            Login = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Name = reader.GetString(3),
            Phone = reader.GetString(4),
            Role = (UserRole)reader.GetInt32(5),
            IsBlocked = reader.GetInt32(6) != 0
        };
    }
}
