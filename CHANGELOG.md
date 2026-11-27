# Changelog

All notable changes to SMSBridge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

- Esendex Message Dispatcher outbound SMS provider with fluent and configuration-based registration (#25).

- Textmagic REST API V2 outbound SMS provider with fluent and configuration-based registration (#26).

- seven.io outbound SMS provider with fluent and configuration-based registration (#27).

- Arkesel SMS V2 outbound provider with header-based authentication and per-recipient result mapping (#57).

- .NET 11 support for the SDK, samples, CI and release publishing. The full test suite runs on .NET 10 and .NET 11.
- NuGet package icon (#64)
- Documentation site logo and favicon (#65)

### Changed

- Polished the GitHub Pages documentation design (#66)

### Removed

- **Breaking:** The NuGet package now requires .NET 10 or later. Applications
  targeting .NET 8 or 9 should stay on the latest compatible SMSBridge release
  until they can upgrade their target framework.

## [0.5.0] - 2026-07-08

### Added

- AWS SNS SMS provider, including deterministic SigV4 signing coverage
- Unifonic SMS provider aligned with the current send-message API documentation
- Africa's Talking SMS provider using the documented legacy SMS endpoint
- GitHub Pages documentation landing page and Jekyll front matter for docs pages

## [0.4.0] - 2026-07-03

### Added

- SmartSMSSolutions SMS provider using the API-x endpoint, with response mapping,
  dependency injection registration, and tests
- Termii SMS provider using the Messaging API, with response mapping, dependency
  injection registration, and tests
- .NET 10 support for the SDK package, samples, tests, CI, and release publishing while preserving .NET 8 and .NET 9 targets.

### Changed

- Updated sample OpenAPI dependencies to avoid the vulnerable `Microsoft.OpenApi` transitive version
- Updated provider documentation and README support tables

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

## [0.2.0] - 2026-06-14

### Added

- Sinch SMS provider (`UseSinch()`) using the XMS Batches REST API with Bearer token authentication
- Plivo SMS provider (`UsePlivo()`) using the REST Message API with Basic authentication
- Telnyx SMS provider (`UseTelnyx()`) using the v2 Messages API with Bearer token authentication

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
