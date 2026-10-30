# Reference comparison evidence

Retrieved 30 October 2026 from Aladhan's public API. Each prayer sample includes its exact source URL, requested method/school/high-latitude/timezone, raw reference times and local calculator differences in minutes. Responses with HTTP 429 are explicitly unverified, not passes. Requests stopped after this bounded batch; no repeated throttled calls.

Successful Cairo/New York/London/Oslo comparisons differ by 0-2 minutes, depending on prayer, date and method. These are not exact matches. Mohamed explicitly reaffirmed on 30 October 2026 that Wathkkir is the correct formula/rounding; these differences are expected, not a defect or open item. The app deliberately uses the previously selected Wathkkir local solar formula and nearest-minute rounding, with sunrise rounded down. Unsupported local methods continue to use Aladhan's response. No arithmetic change is made merely to force this audit to pass.

Hijri reference: https://api.aladhan.com/v1/gToH/30-10-2026?calendarMethod=UAQ returns 19 Jumada al-Ula 1448. The app's calendar uses .NET UmAlQuraCalendar; round-trip tests cover 2026. Moon-sighting/local-calendar differences remain possible.
