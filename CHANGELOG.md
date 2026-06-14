# Changelog

All notable changes to SMSBridge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

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
