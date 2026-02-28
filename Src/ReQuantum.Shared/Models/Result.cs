namespace ReQuantum.Shared.Models;

public record Result(bool IsSuccess, FormattableString? Message = null)
{
    public static Result Success() => new(true);
    public static Result Success(FormattableString? message) => new(true, message);
    public static Result Failure(FormattableString? message = null) => new(false, message);

    public static Result Success(string message) => new(true, $"{message}");
    public static Result Failure(string message) => new(false, $"{message}");

    public static implicit operator Result(bool isSuccess) => new(isSuccess);
}

public record Result<T>(bool IsSuccess, T? Value = default, FormattableString? Message = null)
{
    public static Result<T> Success(T value, FormattableString? message = null) => new(true, value, message);
    public static Result<T> Failure(FormattableString? message = null) => new(false, default, message);

    public static Result<T> Success(T value, string message) => new(true, value, $"{message}");
    public static Result<T> Failure(string message) => new(false, default, $"{message}");

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result(Result<T> result) => new(result.IsSuccess, result.Message);
}
