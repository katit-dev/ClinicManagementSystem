namespace ClinicManagementSystem.Application.Helpers;

public static class HelperFunction
{
    public static string HashPassword(string password, int workFactor = 12)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                "Password cannot be empty",
                nameof(password));
        }

        if (workFactor < 4 || workFactor > 31)
        {
            throw new ArgumentException(
                "Work factor must be between 4 and 31",
                nameof(workFactor));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                "Password cannot be empty",
                nameof(password));
        }

        if (string.IsNullOrEmpty(hashedPassword))
        {
            throw new ArgumentException(
                "Hash cannot be empty",
                nameof(hashedPassword));
        }

        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}