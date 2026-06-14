using SmsBridge.Abstractions;
using SmsBridge.Validation;

namespace SmsBridge.UnitTests;

public sealed class SmsMessageValidatorTests
{
    [Fact]
    public void Validate_ThrowsWhenToIsMissing()
    {
        var message = new SmsMessage { To = "", Body = "Hello" };

        var act = () => SmsMessageValidator.Validate(message);

        act.Should().Throw<SmsBridgeException>().WithMessage("*'To' is required*");
    }

    [Fact]
    public void Validate_ThrowsWhenBodyIsMissing()
    {
        var message = new SmsMessage { To = "+447700900001", Body = "" };

        var act = () => SmsMessageValidator.Validate(message);

        act.Should().Throw<SmsBridgeException>().WithMessage("*'Body' is required*");
    }

    [Fact]
    public void Validate_ThrowsWhenMessageIsNull()
    {
        var act = () => SmsMessageValidator.Validate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_DoesNotThrowForValidMessage()
    {
        var message = new SmsMessage { To = "+447700900001", Body = "Hello" };

        var act = () => SmsMessageValidator.Validate(message);

        act.Should().NotThrow();
    }
}
