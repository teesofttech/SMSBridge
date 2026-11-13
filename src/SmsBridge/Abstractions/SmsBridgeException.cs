namespace SmsBridge.Abstractions;

/// <summary>
/// Thrown for SDK-level configuration, validation, and internal errors.
/// Not thrown for normal provider failures — those are captured in <see cref="SmsSendResult"/>.
/// </summary>
public class SmsBridgeException : Exception
{
    public SmsBridgeException(string message) : base(message) { }

    public SmsBridgeException(string message, Exception innerException)
        : base(message, innerException) { }
}
