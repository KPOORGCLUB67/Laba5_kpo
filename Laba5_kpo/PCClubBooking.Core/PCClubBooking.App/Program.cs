
using System.Globalization;
using System.Text;

using PCClubBooking.Core.Models;
using PCClubBooking.Core.Services;

using PCClubBooking.Data;
using PCClubBooking.Data.Repositories;

internal static class Program
{
    private static readonly CultureInfo RuCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            // 1. Создание базы данных

            var databasePath = Path.Combine(
                AppContext.BaseDirectory,
                "club.db"
            );

            var database = new Database(databasePath);

            // 2. Создание репозиториев

            var users = new SqliteUserRepository(database);
            var pcs = new SqlitePcRepository(database);
            var bookings = new SqliteBookingRepository(database);

            // 3. Создание тарифов

            var tariffs = new TariffService(new[]
            {
                new Tariff
                {
                    Category = PcCategory.Standard,
                    PricePerHour = 150,
                    DiscountPercent = 0
                },

                new Tariff
                {
                    Category = PcCategory.Gaming,
                    PricePerHour = 250,
                    DiscountPercent = 5
                },

                new Tariff
                {
                    Category = PcCategory.Vip,
                    PricePerHour = 400,
                    DiscountPercent = 10
                }
            });

            // 4. Создание сервисов

            var authService = new AuthService(users);

            var bookingService = new BookingService(
                bookings,
                pcs,
                tariffs
            );

            var reportService = new ReportService(bookings);

            var fileExportService = new FileExportService(
                Path.Combine(AppContext.BaseDirectory, "reports")
            );

            var reportController = new ReportController(
                reportService,
                fileExportService
            );

            // 5. Добавление начальных данных

            SeedPcs(pcs);

            SeedDemoAccounts(authService, users);

            // 6. Запуск главного меню

            Console.WriteLine("==================================");
            Console.WriteLine("       PC CLUB BOOKING SYSTEM");
            Console.WriteLine("==================================");

            Console.WriteLine("Тестовый клиент: client / client123");
            Console.WriteLine("Администратор: admin / admin123");

            Console.WriteLine();

            MainMenu(
                authService,
                pcs,
                bookings,
                bookingService,
                reportController
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка запуска приложения:");
            Console.WriteLine(ex.Message);
        }
    }

    // Добавление демонстрационных компьютеров

    private static void SeedPcs(SqlitePcRepository pcs)
    {
        if (pcs.GetAll().Any())
            return;

        pcs.Add(new Pc
        {
            Number = "PC-01",
            Cpu = "Intel Core i5-12400F",
            Gpu = "RTX 3060",
            RamGb = 16,
            Monitor = "24 дюйма, 1920x1080",
            Periphery = "Мышь, клавиатура, наушники",
            Category = PcCategory.Standard,
            Status = PcStatus.Free
        });

        pcs.Add(new Pc
        {
            Number = "PC-02",
            Cpu = "Intel Core i7-13700K",
            Gpu = "RTX 4070",
            RamGb = 32,
            Monitor = "27 дюймов, 2560x1440",
            Periphery = "Мышь, клавиатура, гарнитура",
            Category = PcCategory.Gaming,
            Status = PcStatus.Free
        });

        pcs.Add(new Pc
        {
            Number = "PC-03",
            Cpu = "Intel Core i9",
            Gpu = "RTX 4080",
            RamGb = 32,
            Monitor = "32 дюйма, 2560x1440",
            Periphery = "Премиальная периферия",
            Category = PcCategory.Vip,
            Status = PcStatus.Free
        });
    }

    // Создание демонстрационных аккаунтов

    private static void SeedDemoAccounts(
        AuthService auth,
        SqliteUserRepository users)
    {
        if (users.FindByLogin("client") == null)
        {
            auth.Register(
                "client",
                "client123",
                "Тестовый клиент",
                "+70000000001"
            );
        }

        if (users.FindByLogin("admin") == null)
        {
            auth.Register(
                "admin",
                "admin123",
                "Администратор",
                "+70000000002",
                UserRole.Admin
            );
        }
    }

    // Главное меню

    private static void MainMenu(
        AuthService auth,
        SqlitePcRepository pcs,
        SqliteBookingRepository bookings,
        BookingService bookingService,
        ReportController reportController)
    {
        while (true)
        {
            Console.WriteLine("\n========== ГЛАВНОЕ МЕНЮ ==========");
            Console.WriteLine("1. Регистрация");
            Console.WriteLine("2. Авторизация");
            Console.WriteLine("0. Выход");

            Console.Write("Выберите действие: ");

            switch (Console.ReadLine())
            {
                case "1":
                    Register(auth);
                    break;

                case "2":
                    var user = Login(auth);

                    if (user != null)
                    {
                        UserMenu(
                            user,
                            pcs,
                            bookings,
                            bookingService,
                            reportController
                        );
                    }
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("Неверный пункт меню.");
                    break;
            }
        }
    }

    // Регистрация

    private static void Register(AuthService auth)
    {
        Console.WriteLine("\n========== РЕГИСТРАЦИЯ ==========");

        var login = ReadRequired("Логин: ");

        var password = ReadRequired("Пароль: ");

        var name = ReadRequired("Имя: ");

        var phone = ReadRequired("Телефон: ");

        try
        {
            var user = auth.Register(
                login,
                password,
                name,
                phone
            );

            Console.WriteLine(
                $"Пользователь создан. ID = {user.Id}"
            );
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is InvalidOperationException)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }

    // Авторизация

    private static User? Login(AuthService auth)
    {
        Console.WriteLine("\n========== АВТОРИЗАЦИЯ ==========");

        var login = ReadRequired("Логин: ");

        var password = ReadRequired("Пароль: ");

        try
        {
            var user = auth.Login(login, password);

            Console.WriteLine(
                $"Здравствуйте, {user.Name}!"
            );

            return user;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine("Ошибка входа: " + ex.Message);

            return null;
        }
    }

    // Меню пользователя

    private static void UserMenu(
        User user,
        SqlitePcRepository pcs,
        SqliteBookingRepository bookings,
        BookingService bookingService,
        ReportController reportController)
    {
        while (true)
        {
            Console.WriteLine("\n========== МЕНЮ ПОЛЬЗОВАТЕЛЯ ==========");

            Console.WriteLine("1. Посмотреть список компьютеров");

            if (user.Role == UserRole.Client)
            {
                Console.WriteLine("2. Забронировать компьютер");
                Console.WriteLine("3. Мои бронирования");
                Console.WriteLine("4. Отменить бронирование");
            }

            if (user.Role == UserRole.Admin)
            {
                Console.WriteLine("5. Сформировать отчёт");
            }

            Console.WriteLine("0. Выйти из аккаунта");

            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ShowPcs(pcs);
                    break;

                case "2" when user.Role == UserRole.Client:
                    CreateBooking(user, pcs, bookingService);
                    break;

                case "3" when user.Role == UserRole.Client:
                    ShowBookings(user, bookings, pcs);
                    break;

                case "4" when user.Role == UserRole.Client:
                    CancelBooking(user, bookings, bookingService, pcs);
                    break;

                case "5" when user.Role == UserRole.Admin:
                    ExportReport(reportController);
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("Неверный пункт меню.");
                    break;
            }
        }
    }

    // Просмотр компьютеров

    private static void ShowPcs(SqlitePcRepository pcs)
    {
        Console.WriteLine("\n========== СПИСОК ПК ==========");

        var items = pcs.GetAll().ToList();

        if (items.Count == 0)
        {
            Console.WriteLine("Компьютеры отсутствуют.");
            return;
        }

        foreach (var pc in items)
        {
            Console.WriteLine(
                $"ID: {pc.Id} | {pc.Number} | " +
                $"{pc.Category} | {pc.Status}"
            );

            Console.WriteLine(
                $"CPU: {pc.Cpu}; GPU: {pc.Gpu}; " +
                $"RAM: {pc.RamGb} GB"
            );
        }
    }

    // Создание бронирования

    private static void CreateBooking(
        User user,
        SqlitePcRepository pcs,
        BookingService bookingService)
    {
        Console.WriteLine("\n========== СОЗДАНИЕ БРОНИ ==========");

        ShowPcs(pcs);

        var pcId = ReadInt("Введите ID компьютера: ");

        var start = ReadDateTime(
            "Начало (дд.мм.гггг чч:мм): "
        );

        var end = ReadDateTime(
            "Окончание (дд.мм.гггг чч:мм): "
        );

        Console.WriteLine("\nДополнительные услуги:");

        Console.WriteLine("0. Без услуги");

        Console.WriteLine("1. Напиток — 50 руб.");

        Console.WriteLine("2. Снек — 80 руб.");

        Console.WriteLine("3. Аренда геймпада — 100 руб.");

        var serviceId = ReadInt("Выберите услугу: ");

        var selectedServices = serviceId switch
        {
            0 => new List<Service>(),

            1 => new List<Service>
            {
                new Service { Id = 1, Name = "Напиток", Price = 50 }
            },

            2 => new List<Service>
            {
                new Service { Id = 2, Name = "Снек", Price = 80 }
            },

            3 => new List<Service>
            {
                new Service
                {
                    Id = 3,
                    Name = "Геймпад",
                    Price = 100
                }
            },

            _ => null
        };

        if (selectedServices == null)
        {
            Console.WriteLine("Неизвестная услуга.");
            return;
        }

        try
        {
            var booking = bookingService.Create(
                user.Id,
                pcId,
                start,
                end,
                selectedServices
            );

            Console.WriteLine("\nБронирование создано!");

            Console.WriteLine($"Номер: {booking.Id}");

            Console.WriteLine(
                $"Стоимость: {booking.TotalPrice:F2} руб."
            );
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is InvalidOperationException)
        {
            Console.WriteLine("Ошибка бронирования: " + ex.Message);
        }
    }

    // Просмотр своих бронирований

    private static void ShowBookings(
        User user,
        SqliteBookingRepository bookings,
        SqlitePcRepository pcs)
    {
        Console.WriteLine("\n========== МОИ БРОНИРОВАНИЯ ==========");

        var items = bookings.GetByUser(user.Id).ToList();

        if (items.Count == 0)
        {
            Console.WriteLine("Бронирований пока нет.");
            return;
        }

        foreach (var booking in items)
        {
            var pcName = pcs.FindById(booking.PcId)?.Number
                ?? $"ПК ID={booking.PcId}";

            var status = booking.IsCancelled
                ? "ОТМЕНЕНО"
                : "АКТИВНО";

            Console.WriteLine(
                $"Бронь #{booking.Id} | {pcName} | " +
                $"{booking.Start:dd.MM.yyyy HH:mm} - " +
                $"{booking.End:dd.MM.yyyy HH:mm} | " +
                $"{booking.TotalPrice:F2} руб. | {status}"
            );
        }
    }

    // Отмена бронирования

    private static void CancelBooking(
        User user,
        SqliteBookingRepository bookings,
        BookingService bookingService,
        SqlitePcRepository pcs)
    {
        Console.WriteLine("\n========== ОТМЕНА БРОНИ ==========");

        ShowBookings(user, bookings, pcs);

        var id = ReadInt(
            "Введите ID бронирования для отмены: "
        );

        try
        {
            bookingService.Cancel(id, user.Id);

            Console.WriteLine("Бронирование успешно отменено.");
        }
        catch (Exception ex) when (
            ex is ArgumentException ||
            ex is InvalidOperationException ||
            ex is UnauthorizedAccessException)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }

    // Формирование CSV-отчёта

    private static void ExportReport(
        ReportController reportController)
    {
        Console.WriteLine("\n========== ОТЧЁТ ПО ВЫРУЧКЕ ==========");

        var fromDate = ReadDate(
            "Начальная дата (дд.мм.гггг): "
        ).Date;

        var toDate = ReadDate(
            "Конечная дата (дд.мм.гггг): "
        ).Date.AddDays(1).AddTicks(-1);

        try
        {
            var result =
                reportController.GenerateAndExportRevenueReport(
                    fromDate,
                    toDate
                );

            Console.WriteLine("\nОтчёт сформирован!");

            Console.WriteLine(
                $"Файл: {result.FilePath}"
            );

            Console.WriteLine(
                $"Общая выручка: {result.TotalRevenue:F2} руб."
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка формирования отчёта: " + ex.Message);
        }
    }

    // Ввод обязательной строки

    private static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            var value = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();

            Console.WriteLine("Поле не должно быть пустым.");
        }
    }

    // Ввод целого числа

    private static int ReadInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            if (int.TryParse(Console.ReadLine(), out var value))
                return value;

            Console.WriteLine("Введите целое число.");
        }
    }

    // Ввод даты и времени

    private static DateTime ReadDateTime(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            if (DateTime.TryParseExact(
                Console.ReadLine(),
                "dd.MM.yyyy HH:mm",
                RuCulture,
                DateTimeStyles.None,
                out var value))
            {
                return value;
            }

            Console.WriteLine(
                "Пример правильного ввода: 15.11.2026 18:30"
            );
        }
    }

    // Ввод даты

    private static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            if (DateTime.TryParseExact(
                Console.ReadLine(),
                "dd.MM.yyyy",
                RuCulture,
                DateTimeStyles.None,
                out var value))
            {
                return value;
            }

            Console.WriteLine(
                "Пример правильного ввода: 15.11.2026"
            );
        }
    }
}
