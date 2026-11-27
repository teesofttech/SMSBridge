# Contributing to SMSBridge

Thank you for your interest in contributing.

## Getting Started

Install the .NET 11 SDK to build the solution. Install the .NET 10 runtime as
well to run the test suites on both supported frameworks.

1. Fork the repository.
2. Clone your fork.
3. Create a feature branch from `main`.
4. Run `dotnet build` to verify the build.
5. Run `dotnet test` to verify all tests pass.
6. Make your changes.
7. Add tests for any new behaviour.
8. Submit a pull request.

## Adding a New Provider

Providers live in `src/SmsBridge/Providers/<ProviderName>/`.

Each provider needs:

- `<Name>SmsProvider.cs` — implements `ISmsProvider`
- `<Name>SmsRequestMapper.cs` — maps `SmsMessage` to the provider's API format
- `<Name>SmsResponseMapper.cs` — maps the provider API response to `SmsSendResult`
- `<Name>Options.cs` in `Options/`
- A `<Name>` value in `Abstractions/SmsProviderType.cs`
- A `<Name>` constant in `Internal/Http/HttpClientNames.cs` (`"SmsBridge.<Name>"`)
- A `Use<Name>()` extension method and `<Name>ProviderConfig` class, either in
  `DependencyInjection/SmsBridgeProviderBuilder.cs` or in their own file under
  `DependencyInjection/Providers/` (see `SmartSmsProviderBuilder.cs`), keeping the
  `SmsBridge.DependencyInjection` namespace
- `<Name>ProviderTests.cs` and `<Name>ResponseMapperTests.cs` in `tests/SmsBridge.ProviderTests/`
- An `ISmsClient_CanBeResolvedFromDI_With<Name>` test in
  `tests/SmsBridge.IntegrationTests/DependencyInjectionTests.cs`
- Optional: a webhook parser in `Webhooks/` implementing `ISmsWebhookParser`, registered in
  `RegisterCore` (`DependencyInjection/SmsBridgeServiceCollectionExtensions.cs`), with tests in
  `tests/SmsBridge.WebhookTests/`
- Documentation: the README "Supported Providers" table, `docs/providers.md`, and the
  provider list in `docs/index.md`

Use `HttpClient` via `IHttpClientFactory`. Do not depend on the provider's official SDK.

## Code Style

- Follow existing patterns.
- Use `internal` for all provider implementations.
- Do not throw for normal provider failures — return `SmsSendResult.Failed(...)`.
- Do not log API keys, tokens, or message body content by default.

## Tests

Use xUnit, AwesomeAssertions, and NSubstitute. Mock HTTP with `RichardSzalay.MockHttp`.

## Commit Style

Write short, clear commit messages that describe the change.

## Licence

By contributing you agree that your contributions will be licensed under the MIT licence.
