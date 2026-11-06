
using Microsoft.Data.Sqlite;
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;

namespace PCClubBooking.Data.Repositories;

public class SqlitePcRepository : IPcRepository
{
    private readonly Database _database;

    public SqlitePcRepository(Database database)
    {
        _database = database;
    }

    public Pc? FindById(int id)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Number, Cpu, Gpu, RamGb,
                   Monitor, Periphery, Category, Status
            FROM Pcs
            WHERE Id = $id
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();

        return reader.Read() ? ReadPc(reader) : null;
    }

    public IEnumerable<Pc> GetAll()
    {
        var result = new List<Pc>();

        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT Id, Number, Cpu, Gpu, RamGb,
                   Monitor, Periphery, Category, Status
            FROM Pcs
            ORDER BY Id;
            """;

        using var reader = command.ExecuteReader();

        while (reader.Read())
            result.Add(ReadPc(reader));

        return result;
    }

    public int Add(Pc pc)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Pcs(
                Number, Cpu, Gpu, RamGb,
                Monitor, Periphery, Category, Status
            )
            VALUES(
                $number, $cpu, $gpu, $ram,
                $monitor, $periphery, $category, $status
            );

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$number", pc.Number);
        command.Parameters.AddWithValue("$cpu", pc.Cpu);
        command.Parameters.AddWithValue("$gpu", pc.Gpu);
        command.Parameters.AddWithValue("$ram", pc.RamGb);
        command.Parameters.AddWithValue("$monitor", pc.Monitor);
        command.Parameters.AddWithValue("$periphery", pc.Periphery);
        command.Parameters.AddWithValue("$category", (int)pc.Category);
        command.Parameters.AddWithValue("$status", (int)pc.Status);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Pc pc)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Pcs
            SET Number=$number,
                Cpu=$cpu,
                Gpu=$gpu,
                RamGb=$ram,
                Monitor=$monitor,
                Periphery=$periphery,
                Category=$category,
                Status=$status
            WHERE Id=$id;
            """;

        command.Parameters.AddWithValue("$number", pc.Number);
        command.Parameters.AddWithValue("$cpu", pc.Cpu);
        command.Parameters.AddWithValue("$gpu", pc.Gpu);
        command.Parameters.AddWithValue("$ram", pc.RamGb);
        command.Parameters.AddWithValue("$monitor", pc.Monitor);
        command.Parameters.AddWithValue("$periphery", pc.Periphery);
        command.Parameters.AddWithValue("$category", (int)pc.Category);
        command.Parameters.AddWithValue("$status", (int)pc.Status);
        command.Parameters.AddWithValue("$id", pc.Id);

        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM Pcs WHERE Id=$id;
            """;

        command.Parameters.AddWithValue("$id", id);

        command.ExecuteNonQuery();
    }

    private static Pc ReadPc(SqliteDataReader reader)
    {
        return new Pc
        {
            Id = reader.GetInt32(0),
            Number = reader.GetString(1),
            Cpu = reader.GetString(2),
            Gpu = reader.GetString(3),
            RamGb = reader.GetInt32(4),
            Monitor = reader.GetString(5),
            Periphery = reader.GetString(6),
            Category = (PcCategory)reader.GetInt32(7),
            Status = (PcStatus)reader.GetInt32(8)
        };
    }
}
