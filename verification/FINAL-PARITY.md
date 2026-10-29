# Fluent Prayer Times verification

Reconstructed from the owner's shipped 1.3.6 application and recovered historical source. The standalone native WinUI 3 source is in `src/FluentPrayerTimes`.

## Passed

- 22,250 differential checks on unmodified Windows assemblies: solar calculation methods, locations, dates, countdown formatting, all-day prayer/iqama states and settings bounds. Zero mismatches or unexpected exceptions.
- Both original and reconstructed native self-tests passed. Reconstructed suite: 68 PASS results.
- 64 screen-launch cases across original/rebuilt, Arabic/English, light/dark. Screenshots inspected side by side for main, calendar, settings, calculation, sound picker, About, widget and hover card.
- Actual Windows navigation to converter and Adhkar pages passed and screenshots were inspected.
- Standalone source build requires no original binary or source-generation step.
- Bundled .NET, Windows App SDK and VC runtime verified; native app launches without shared Windows App Runtime.
- Portable packaging, per-user installation, installed launch, running-app upgrade, Start menu/desktop shortcuts, uninstall and settings retention passed.

## Intentional differences and fixes

- Repository, About and updater URLs point to StarsDust01/fluent-prayer-times.
- The generated application PRI is explicitly copied during publish, preventing a startup resource crash.
- Installer upgrades signal the app's existing Quit event before replacing running files. The recovered original installer failed that test with files in use.
- Original README wording recovered from historical complete writes and later recorded edits. Lost screenshots are replaced only by fresh screenshots supplied by the owner. The original app icon and audio are preserved.

## Scope limits

This is not a proof of every network response, every Windows version or every interactive path. Anonymous release API access had no latest release to return before publication; live updater download verification must be repeated against the published release.

## Evidence

- Native differential/self-tests and light/dark screens: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40759438806
- Original packaging plus upgrade fix: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40762673696
- Actual converter/Adhkar navigation: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40764185902
- Standalone source build and packaging: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40764144288
