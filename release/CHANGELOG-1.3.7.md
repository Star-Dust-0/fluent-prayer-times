# Fluent Prayer Times 1.3.7

- Fixed automatic location detection by using Windows location services directly instead of a legacy PowerShell helper.
- Prevented location detection from getting stuck when Windows or a network provider does not respond.
- Added a second approximate IP-location fallback when Windows location is unavailable and the first provider fails.
- Preserved saved locations and manual city selection when detection cannot complete.
- Corrected installation to Program Files with an optional desktop shortcut, while preserving settings during upgrades.

IP-based location is approximate and can reflect a VPN or internet provider rather than your precise position.
