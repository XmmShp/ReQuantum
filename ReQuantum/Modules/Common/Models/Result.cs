using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.Infrastructure.Models;

public record FormattableMessage(string TemplateKey, object?[] Arguments)
{
    public override string ToString() => string.Format(TemplateKey, Arguments);
}

public record Result(bool IsSuccess, FormattableMessage Message)
{
    #region Static Helper
    public static implicit operator Result(FailResult result)
    {
        return new Result(false, result.Message);
    }

    public static FailResult Fail(FormattableMessage message)
    {
        return new FailResult(message);
    }

    public static FailResult Fail(string messageTemplateKey, params object?[] messageArgs)
    {
        return new FailResult(new FormattableMessage(messageTemplateKey, messageArgs));
    }

    public static Result Success(string messageTemplateKey, params object?[] messageArgs)
    {
        return new Result(true, new FormattableMessage(messageTemplateKey, messageArgs));
    }

    public static Result<T> Success<T>(T value, string messageTemplateKey = "", params object?[] messageArgs)
    {
        return new Result<T>(true, new FormattableMessage(messageTemplateKey, messageArgs), value);
    }
    #endregion
}

public record FailResult(FormattableMessage Message);


public record Result<T>(
    [property: MemberNotNullWhen(true, nameof(Result<T>.Value))]
    bool IsSuccess,
    FormattableMessage Message,
    T? Value)
{
    public static implicit operator Result<T>(FailResult result)
    {
        return new Result<T>(false, result.Message, default);
    }

    public static implicit operator Result<T>(T value)
        => Result.Success(value);
}
