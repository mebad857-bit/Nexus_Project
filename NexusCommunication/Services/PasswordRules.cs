namespace NexusCommunication.Services;

/// <summary>One place for the password rules, used by change-password and by the reset page (the sign-up page keeps its own check)</summary>
public static class PasswordRules
{
    public const int MinLength = 6;
    public const int MaxLength = 100;

    /// <summary>null when the password is acceptable, otherwise the message to show</summary>
    public static string? Check(string password, string username)
    {
        if (password.Length < MinLength) return $"The new password must be at least {MinLength} characters";
        if (password.Length > MaxLength) return "The new password is too long";
        if (!string.IsNullOrEmpty(username) && string.Equals(password, username, StringComparison.OrdinalIgnoreCase))
            return "The password must not be the same as your username";
        return null;
    }
}
