namespace Algomim.Aec.Mcp.Core;

/// <summary>
/// A result that is either a success carrying a value or a failure carrying an <see cref="Error"/>.
/// </summary>
public readonly struct Result<T>
{
    private readonly bool _isSuccess;
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _isSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        _isSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public T Value => _isSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access value of failed result: {_error}");

    public Error Error => !_isSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access error of successful result.");

    public T GetValueOrDefault(T defaultValue = default!) => _isSuccess ? _value! : defaultValue;

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => _isSuccess ? onSuccess(_value!) : onFailure(_error!);
}

/// <summary>
/// A result that is either a success (no value) or a failure carrying an <see cref="Error"/>.
/// </summary>
public readonly struct Result
{
    private readonly bool _isSuccess;
    private readonly Error? _error;

    private Result(bool isSuccess, Error? error = null)
    {
        _isSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public Error Error => !_isSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access error of successful result.");

    public static Result Success() => new(true);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}
