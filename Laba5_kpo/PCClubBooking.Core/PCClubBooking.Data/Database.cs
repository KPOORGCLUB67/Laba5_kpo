
using Microsoft.Data.Sqlite;

namespace PCClubBooking.Data;

public class Database
{
    private readonly string _connectionString;

    public string DatabasePath { get; }

    public Database(string dbPath = "club.db")
    {
        DatabasePath = Path.GetFullPath(dbPath);

        var directory = Path.GetDirectoryName(DatabasePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath
        }.ToString();

        Initialize();
    }

    public SqliteConnection Open()
    {
        var connection =
            new SqliteConnection(_connectionString);

        connection.Open();

        return connection;
    }

    private void Initialize()
    {
        using var connection = Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Login TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Name TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '',
                Role INTEGER NOT NULL,
                IsBlocked INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Pcs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Number TEXT NOT NULL UNIQUE,
                Cpu TEXT NOT NULL DEFAULT '',
                Gpu TEXT NOT NULL DEFAULT '',
                RamGb INTEGER NOT NULL DEFAULT 0,
                Monitor TEXT NOT NULL DEFAULT '',
                Periphery TEXT NOT NULL DEFAULT '',
                Category INTEGER NOT NULL,
                Status INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Bookings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                PcId INTEGER NOT NULL,
                Start TEXT NOT NULL,
                End TEXT NOT NULL,
                TotalPrice REAL NOT NULL,
                IsCancelled INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS IX_Bookings_PcId
                ON Bookings(PcId);

            CREATE INDEX IF NOT EXISTS IX_Bookings_UserId
                ON Bookings(UserId);

            CREATE TABLE IF NOT EXISTS Services (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price REAL NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }
}
