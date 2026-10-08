namespace NovaBooks.Application.Authentication;

public enum LoginStatus
{
    Succeeded,
    InvalidCredentials,
    LockedOut,
    Disabled
}

public sealed class LoginResult
{
    public LoginStatus Status { get; private init; }

    public LoginResponse? Response { get; private init; }

    public DateTimeOffset? LockoutEndUtc { get; private init; }

    public static LoginResult Success(LoginResponse response)
    {
        return new LoginResult
        {
            Status = LoginStatus.Succeeded,
            Response = response
        };
    }

    public static LoginResult InvalidCredentials()
    {
        return new LoginResult
        {
            Status = LoginStatus.InvalidCredentials
        };
    }

    public static LoginResult LockedOut(
        DateTimeOffset? lockoutEndUtc)
    {
        return new LoginResult
        {
            Status = LoginStatus.LockedOut,
            LockoutEndUtc = lockoutEndUtc
        };
    }

    public static LoginResult Disabled()
    {
        return new LoginResult
        {
            Status = LoginStatus.Disabled
        };
    }
}
