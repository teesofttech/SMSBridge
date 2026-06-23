using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmsBridge.Abstractions;
using SmsBridge.Observability;
using SmsBridge.Options;
using SmsBridge.Routing;
using SmsBridge.Validation;

namespace SmsBridge;

internal sealed class SmsClient : ISmsClient
{
    private readonly FailoverSmsProviderRouter _router;
    private readonly SmsBridgeOptions _options;
    private readonly ILogger<SmsClient> _logger;

    public SmsClient(
        FailoverSmsProviderRouter router,
        IOptions<SmsBridgeOptions> options,
        ILogger<SmsClient> logger)
    {
        _router = router;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        SmsMessageValidator.Validate(message);

        var context = new SmsRoutingContext
        {
            Message = message,
            ExplicitProvider = message.Provider
        };

        var provider = _router.Resolve(context);

        using var activity = SmsBridgeTelemetry.StartSendActivity(provider.Name, message.To);

        _logger.LogInformation("SmsBridge: sending SMS via provider '{Provider}'", provider.Name);

        var result = await provider.SendAsync(message, cancellationToken);

        if (result.Success)
        {
            activity?.SetTag("sms.success", true);
            _logger.LogInformation("SmsBridge: SMS sent successfully via '{Provider}', messageId={MessageId}",
                provider.Name, result.ProviderMessageId);
            return result;
        }

        activity?.SetTag("sms.success", false);
        activity?.SetTag("sms.error", result.ErrorMessage);

        _logger.LogWarning("SmsBridge: SMS send failed via '{Provider}', error={Error}, transient={IsTransient}",
            provider.Name, result.ErrorMessage, result.IsTransientFailure);

        if (result.IsTransientFailure &&
            !result.MayHaveBeenAccepted &&
            _options.EnableFailover &&
            string.IsNullOrWhiteSpace(message.Provider))
        {
            var failover = _router.ResolveFailover();
            if (failover is not null)
            {
                _logger.LogInformation("SmsBridge: attempting failover to provider '{FailoverProvider}'", failover.Name);
                var failoverResult = await failover.SendAsync(message, cancellationToken);

                if (failoverResult.Success)
                    _logger.LogInformation("SmsBridge: failover succeeded via '{FailoverProvider}'", failover.Name);
                else
                    _logger.LogWarning("SmsBridge: failover also failed via '{FailoverProvider}', error={Error}",
                        failover.Name, failoverResult.ErrorMessage);

                return failoverResult;
            }
        }

        return result;
    }
}
