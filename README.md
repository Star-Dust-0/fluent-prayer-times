<div align="center">

# Fluent Prayer Times

A Windows prayer-times app with a native WinUI 3 interface, a tray countdown and a floating prayer widget.

brought to you by **Instinct**

</div>

## Recovery status

This repository is being restored after an account change. The source on `main` is a partial historical recovery, not the verified source of the latest shipped app. Do not build or install it as a replacement yet.

The original project README was not recovered in full. This English README has been rebuilt from the recovered source, the shipped 1.3.6 app, its bundled README, and recorded project changes. It is not presented as the original wording.

Work to recover the full 1.3.6 app and compare it against the original is on `reverse-engineered-v1.3.6`. See `RECOVERY-STATUS.md` for the limits of the historical files on `main`.

## Features

- Prayer times, a tray countdown, adhan and iqama phases, and a floating widget.
- Location search, manual coordinates, calculation settings and time-zone/DST controls.
- Hijri calendar, date converter and Arabic azkar.
- Arabic and English layout directions, with 11 interface languages. Azkar stay Arabic.
- Light, dark and system themes, Mica/Mica Alt/Acrylic backgrounds and accent colors.
- Separate adhan and iqama notifications, sound choices and per-prayer iqama times.
- Local prayer calculation for supported methods, with online services and cached data for other cases.

## Requirements and portable use

The original self-contained 1.3.6 backup targets Windows 10 version 1809 or later and Windows 11, x64.

1. Extract the whole portable ZIP before opening `FluentPrayerTimes.exe`.
2. Keep all files together. Do not copy the executable alone.
3. The self-contained edition includes .NET, Windows App SDK and runtime files. It does not need a separate runtime installation.
4. The original self-contained installer installs for the current user without administrator rights.

These describe the original distribution. A restored replacement download has not been released from this repository yet.

## Downloads and updates

The update-source and project links are being moved to `StarsDust01/fluent-prayer-times`. While this repository is private, an anonymous in-app GitHub API check cannot read its releases. A failed check must not be read as confirmation that the installed app is current.

No token or account password is included in the app. Public availability and the final update flow will be verified before the restored release.

## Links

Repository:

```text
https://github.com/StarsDust01/fluent-prayer-times
```

Reconstruction branch:

```text
https://github.com/StarsDust01/fluent-prayer-times/tree/reverse-engineered-v1.3.6
```

Releases (no restored release yet):

```text
https://github.com/StarsDust01/fluent-prayer-times/releases
```

Prayer-time API used by the app:

```text
https://aladhan.com/prayer-times-api
```

Place search service used by the app:

```text
https://open-meteo.com/
```

Instinct:

```text
https://instinct.com
```

## Build status

The historical root project is incomplete. For the reconstruction build instructions and verification notes, open `reconstruction/README.md` on the reconstruction branch. A successful compile alone is not a claim of original-app parity.

## License

The original 1.3.6 backup includes the MIT License, copyright 2026 Mohamed Elnaggar. Its license text will be included with the restored source and downloads. Third-party dependencies keep their own licenses.
