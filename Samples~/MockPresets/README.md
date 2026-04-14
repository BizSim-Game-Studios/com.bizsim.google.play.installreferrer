# Mock Config Presets

Pre-configured mock configs for testing common install referrer scenarios in the Unity Editor.

## Setup

1. Import this sample via **Package Manager → Install Referrer → Samples → Mock Presets**
2. Run the menu command: **BizSim → Install Referrer → Create Mock Presets**
3. Assign any preset to the `InstallReferrerController` component's **Mock Config** field
4. Enter Play Mode — the controller returns mock data instead of calling the real API

## Available Presets

| Preset | Description | UTM Parameters |
|--------|-------------|----------------|
| **Organic** | Direct install from Play Store (no referrer) | *(empty)* |
| **Google Ads Campaign** | Paid search campaign | `utm_source=google&utm_medium=cpc&utm_campaign=summer_sale` |
| **Facebook Social** | Social media campaign | `utm_source=facebook&utm_medium=social&utm_campaign=launch` |
| **Friend Invitation** | Referral deep link | `utm_source=user_12345&utm_medium=invite&utm_campaign=invite` |
| **Error: Service Unavailable** | Simulates a transient API error | *(error simulation)* |
| **Error: Feature Not Supported** | Simulates a permanent API error | *(error simulation)* |
