---
title: SMSBridge Documentation
layout: default
---

<section class="intro-panel">
  <p class="eyebrow">Provider-agnostic SMS for .NET</p>
  <h2>One clean API for every SMS provider.</h2>
  <p>
    SMSBridge helps .NET applications send SMS through multiple providers,
    switch providers without rewriting application code, and parse delivery
    webhooks through a consistent SDK surface.
  </p>
</section>

<section class="doc-grid" aria-label="Documentation sections">
  <a class="doc-card" href="{{ '/getting-started.html' | relative_url }}">
    <span>01</span>
    <strong>Getting Started</strong>
    <p>Install SMSBridge, register a provider, and send your first SMS.</p>
  </a>
  <a class="doc-card" href="{{ '/providers.html' | relative_url }}">
    <span>02</span>
    <strong>Providers</strong>
    <p>Configure Twilio, Vonage, AWS SNS, Unifonic, Africa's Talking, and more.</p>
  </a>
  <a class="doc-card" href="{{ '/switching-providers.html' | relative_url }}">
    <span>03</span>
    <strong>Switching Providers</strong>
    <p>Change providers without coupling your application to vendor code.</p>
  </a>
  <a class="doc-card" href="{{ '/failover.html' | relative_url }}">
    <span>04</span>
    <strong>Failover</strong>
    <p>Understand when SMSBridge can safely retry through a fallback provider.</p>
  </a>
  <a class="doc-card" href="{{ '/webhooks.html' | relative_url }}">
    <span>05</span>
    <strong>Webhooks</strong>
    <p>Parse delivery status callbacks from supported SMS providers.</p>
  </a>
</section>

<section class="provider-strip" aria-label="Supported providers">
  <h2>Supported Providers</h2>
  <p>
    Twilio, Vonage, Sinch, Plivo, Telnyx, MessageBird, AWS SNS, Infobip,
    SmartSMSSolutions, Termii, Unifonic, and Africa's Talking.
  </p>
</section>

<section class="source-actions" aria-label="Project links">
  <a href="https://github.com/teesofttech/SMSBridge">GitHub repository</a>
  <a href="https://www.nuget.org/packages/SmsBridge">NuGet package</a>
</section>
