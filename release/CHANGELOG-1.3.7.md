# Fluent Prayer Times 1.3.7

- Fixed automatic location detection by using Windows location services directly instead of a legacy PowerShell helper.
- Prevented location detection from getting stuck when Windows or a network provider does not respond.
- Added a second approximate IP-location fallback when Windows location is unavailable and the first provider fails.
- Preserved saved locations and manual city selection when detection cannot complete.
- Corrected installation to Program Files with an optional desktop shortcut, while preserving settings during upgrades.

IP-based location is approximate and can reflect a VPN or internet provider rather than your precise position.

Additional fixes found during the full UI audit:

- Connected the Detect, city Search, and manual-coordinate Apply controls to their handlers.
- Fixed startup registration when the Windows Run registry key is absent.
- Fixed Gregorian/Hijri conversion changing the selected year while switching calendars; corrected the converted date's day label.
- Made the custom accent-color dialog follow the selected app theme.
- Fixed stale translated headings after switching language, and filled missing calculation/update text across the supported languages.

Prayer calculation formulas and Wathkkir rounding are unchanged. Automatic location uses Windows location when available, with approximate network fallbacks. Windows permissions, device capability, VPNs, and internet-provider routing can affect location accuracy.
