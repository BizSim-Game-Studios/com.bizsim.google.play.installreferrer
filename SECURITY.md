# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.2.x   | Yes       |
| 0.1.x   | No        |

## Reporting a Vulnerability

If you discover a security vulnerability in this package, please report it responsibly:

1. **Do not** open a public GitHub issue
2. Email: **security@bizsim.com**
3. Include: package name, version, description of the vulnerability, and steps to reproduce

We will acknowledge your report within 48 hours and provide a fix timeline within 7 days.

## Scope

This package handles install attribution data with the following security considerations:

- **Referrer data** is obtained via local IPC to Google Play — no network calls are made
- **UTM parameters** are user-visible in referral links — never encode rewards or secrets in them
- **Cache storage** supports optional encryption (`_useEncryptedCache` toggle)
- **GDPR compliance** — `ClearCachedData()` and `SetConsentGranted(false)` for data erasure
- **Consent persistence (v1.0.3+)** — `ConsentGranted` state persists across app restarts via `PlayerPrefs` key `BizSim.InstallReferrer.ConsentGranted`. Revoking consent via `SetConsentGranted(false)` survives process termination, domain reload, and device reboot. Prior to v1.0.3 the flag was in-memory only and silently reset to `true` on each boot — a GDPR right-to-erasure gap. Defaults to `true` on fresh install for backward compatibility; consumers must explicitly call `SetConsentGranted(true)` to re-grant after revocation.
- **ProGuard rules** are embedded to prevent reverse engineering of the Java bridge
