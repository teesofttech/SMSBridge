# Changelog

All notable changes to SMSBridge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

## [0.3.0] - 2026-06-23

### Added

- MessageBird SMS provider with documented `AccessKey` authentication, request mapping,
  response/error normalization, dependency injection registration, and sample configuration
- Infobip SMS provider using the current SMS API v3, `App` authentication, response/error
  normalization, dependency injection registration, and sample configuration
- Delivery report parsing for Plivo, Sinch, and Telnyx
- Normalized delivery error codes and transient-failure metadata for provider webhooks
- Optional delivery callback configuration for Twilio, Plivo, and Sinch
- Configurable regional Sinch SMS API base URL

### Changed

- Aligned provider request, response, and retry behavior with current official documentation
- Added automatic Unicode selection for Vonage messages outside GSM-7
- Made automatic failover conservative for ambiguous network and provider `5xx` outcomes
- Updated provider, webhook, failover, and README documentation

## [0.1.0] - 2026-06-14

### Added

- Initial SDK foundation
- `ISmsClient` — the single interface application code depends on
- `SmsMessage`, `SmsSendResult`, `SmsDeliveryStatus`, `SmsProviderType`
- Twilio provider (outbound SMS via REST API)
- Vonage provider (outbound SMS via REST API)
- Configuration-based provider selection
- Per-message provider override
- Failover support (transient failure triggers automatic retry via backup provider)
- ASP.NET Core dependency injection support via `AddSmsBridge()` and fluent builder
- Basic logging via `ILogger<T>`
- OpenTelemetry-friendly activity tracing around send operations
- Webhook event normalisation for Twilio and Vonage
- Unit tests, provider tests, webhook tests, integration tests
- Sample projects: BasicSample, SwitchProviderSample, FailoverSample
- GitHub Actions CI workflow
