
using PCClubBooking.Core.Models;
using PCClubBooking.Core.Repositories;

namespace PCClubBooking.Core.Services;

public class AuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users)
    {
        _users = users ??
            throw new ArgumentNullException(nameof(users));
    }

    public User Register(
        string login,
        string password,
        string name,
        string phone,
        UserRole role = UserRole.Client)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException(
                "Логин не может быть пустым.");

        if (string.IsNullOrWhiteSpace(password) ||
            password.Length < 4)
            throw new ArgumentException(
                "Пароль должен содержать не менее 4 символов.");

        login = login.Trim();

        if (_users.FindByLogin(login) != null)
            throw new InvalidOperationException(
                "Пользователь с таким логином уже существует.");

        var user = new User
        {
            Login = login,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(password),

            Name = (name ?? string.Empty).Trim(),

            Phone = (phone ?? string.Empty).Trim(),

            Role = role,

            IsBlocked = false
        };

        user.Id = _users.Add(user);

        return user;
    }

    public User Login(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrEmpty(password))
            throw new UnauthorizedAccessException(
                "Неверный логин или пароль.");

        var user = _users.FindByLogin(login.Trim());

        if (user == null)
            throw new UnauthorizedAccessException(
                "Неверный логин или пароль.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException(
                "Пользователь заблокирован.");

        if (!BCrypt.Net.BCrypt.Verify(
                password,
                user.PasswordHash))
            throw new UnauthorizedAccessException(
                "Неверный логин или пароль.");

        return user;
    }
}
