
using Microsoft.Data.Sqlite;
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;

namespace PCClubBooking.Data;

public class SqliteBookingRepository : IBookingRepository
{
    private readonly Database _database;

    public SqliteBookingRepository(Database database)
    {
        _database = database;
    }

    public int Add(Booking booking)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Bookings(
                UserId, PcId, Start, End, TotalPrice, IsCancelled
            )
            VALUES(
                $userId, $pcId, $start, $end, $price, $cancelled
            );

            SELECT last_insert_rowid();
            """;

        command.Parameters.AddWithValue("$userId", booking.UserId);
        command.Parameters.AddWithValue("$pcId", booking.PcId);
        command.Parameters.AddWithValue("$start", booking.Start.ToString("O"));
        command.Parameters.AddWithValue("$end", booking.End.ToString("O"));
        command.Parameters.AddWithValue("$price", booking.TotalPrice);
        command.Parameters.AddWithValue(
            "$cancelled",
            booking.IsCancelled ? 1 : 0
        );

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Update(Booking booking)
    {
        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            UPDATE Bookings
            SET IsCancelled=$cancelled
            WHERE Id=$id;
            """;

        command.Parameters.AddWithValue(
            "$cancelled",
            booking.IsCancelled ? 1 : 0
        );

        command.Parameters.AddWithValue("$id", booking.Id);

        command.ExecuteNonQuery();
    }

    public IEnumerable<Booking> GetByUser(int userId)
    {
        return Query("WHERE UserId=$value", "$value", userId);
    }

    public IEnumerable<Booking> GetActive()
    {
        return Query("WHERE IsCancelled=0", null, null);
    }

    public bool IsIntervalBusy(
        int pcId,
        DateTime start,
        DateTime end)
    {
        return GetActive().Any(b =>
            b.PcId == pcId &&
            start < b.End &&
            end > b.Start);
    }

    private IEnumerable<Booking> Query(
        string where,
        string? parameterName,
        object? parameterValue)
    {
        var result = new List<Booking>();

        using var connection = _database.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT Id, UserId, PcId, Start, End,
                   TotalPrice, IsCancelled
            FROM Bookings
            {where}
            ORDER BY Start;
            """;

        if (parameterName != null)
        {
            command.Parameters.AddWithValue(
                parameterName,
                parameterValue!
            );
        }

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new Booking
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                PcId = reader.GetInt32(2),

                Start = DateTime.Parse(reader.GetString(3)),

                End = DateTime.Parse(reader.GetString(4)),

                TotalPrice = Convert.ToDecimal(reader.GetDouble(5)),

                IsCancelled = reader.GetInt32(6) != 0
            });
        }

        return result;
    }
}
