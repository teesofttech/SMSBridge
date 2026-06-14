using Microsoft.Extensions.Options;
using SmsBridge.Abstractions;
using SmsBridge.Options;

namespace SmsBridge.Validation;

internal sealed class SmsBridgeOptionsValidator : IValidateOptions<SmsBridgeOptions>
{
    public ValidateOptionsResult Validate(string? name, SmsBridgeOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DefaultProvider))
            failures.Add("'SmsBridge:DefaultProvider' must be set.");

        if (options.Providers.Count == 0)
            failures.Add("At least one provider must be configured under 'SmsBridge:Providers'.");

        if (!string.IsNullOrWhiteSpace(options.DefaultProvider) &&
            !options.Providers.ContainsKey(options.DefaultProvider))
        {
            failures.Add($"Default provider '{options.DefaultProvider}' is not configured. " +
                         $"Configured providers: [{string.Join(", ", options.Providers.Keys)}].");
        }

        if (options.EnableFailover)
        {
            if (string.IsNullOrWhiteSpace(options.FailoverProvider))
                failures.Add("'SmsBridge:FailoverProvider' must be set when failover is enabled.");
            else if (!options.Providers.ContainsKey(options.FailoverProvider))
                failures.Add($"Failover provider '{options.FailoverProvider}' is not configured.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
