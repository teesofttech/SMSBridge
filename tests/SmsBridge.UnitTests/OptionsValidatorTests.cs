using SmsBridge.Options;
using SmsBridge.Validation;

namespace SmsBridge.UnitTests;

public sealed class OptionsValidatorTests
{
    private readonly SmsBridgeOptionsValidator _validator = new();

    [Fact]
    public void Validate_FailsWhenDefaultProviderIsEmpty()
    {
        var opts = new SmsBridgeOptions
        {
            DefaultProvider = "",
            Providers = new Dictionary<string, SmsProviderOptions>
            {
                ["twilio"] = new SmsProviderOptions { Type = Abstractions.SmsProviderType.Twilio }
            }
        };

        var result = _validator.Validate(null, opts);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_FailsWhenNoProviders()
    {
        var opts = new SmsBridgeOptions { DefaultProvider = "twilio" };

        var result = _validator.Validate(null, opts);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_FailsWhenDefaultProviderNotInProviders()
    {
        var opts = new SmsBridgeOptions
        {
            DefaultProvider = "twilio",
            Providers = new Dictionary<string, SmsProviderOptions>
            {
                ["vonage"] = new SmsProviderOptions { Type = Abstractions.SmsProviderType.Vonage }
            }
        };

        var result = _validator.Validate(null, opts);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("twilio"));
    }

    [Fact]
    public void Validate_FailsWhenFailoverEnabledButProviderMissing()
    {
        var opts = new SmsBridgeOptions
        {
            DefaultProvider = "twilio",
            EnableFailover = true,
            FailoverProvider = "",
            Providers = new Dictionary<string, SmsProviderOptions>
            {
                ["twilio"] = new SmsProviderOptions { Type = Abstractions.SmsProviderType.Twilio }
            }
        };

        var result = _validator.Validate(null, opts);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_SucceedsWithValidConfiguration()
    {
        var opts = new SmsBridgeOptions
        {
            DefaultProvider = "twilio",
            Providers = new Dictionary<string, SmsProviderOptions>
            {
                ["twilio"] = new SmsProviderOptions { Type = Abstractions.SmsProviderType.Twilio }
            }
        };

        var result = _validator.Validate(null, opts);

        result.Succeeded.Should().BeTrue();
    }
}
