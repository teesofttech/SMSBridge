---
title: SMSBridge Documentation
---

# SMSBridge Documentation

SMSBridge is a provider-agnostic .NET SDK for sending SMS messages through
multiple providers using one clean API.

## Start Here

- [Getting Started](getting-started.md): install SMSBridge, register a provider,
  and send your first SMS.
- [Providers](providers.md): configure supported SMS providers.
- [Switching Providers](switching-providers.md): change providers without
  rewriting application code.
- [Failover](failover.md): understand when SMSBridge can safely retry through a
  fallback provider.
- [Webhooks](webhooks.md): parse delivery status callbacks from supported
  providers.

## Supported Providers

SMSBridge currently supports Twilio, Vonage, Sinch, Plivo, Telnyx, MessageBird,
AWS SNS, Infobip, SmartSMSSolutions, Termii, Unifonic, and Africa's Talking.

## Source

- [GitHub repository](https://github.com/teesofttech/SMSBridge)
- [NuGet package](https://www.nuget.org/packages/SmsBridge)
