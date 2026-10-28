# Fluent Prayer Times verification

Reconstructed from the owner's shipped 1.3.6 application and recovered historical source. The standalone native WinUI 3 source is in `src/FluentPrayerTimes`.

## Passed

- 22,250 differential checks on unmodified Windows assemblies: solar calculation methods, locations, dates, countdown formatting, all-day prayer/iqama states and settings bounds. Zero mismatches or unexpected exceptions.
- Both original and reconstructed native self-tests passed. Reconstructed suite: 68 PASS results.
- 64 screen-launch cases across original/rebuilt, Arabic/English, light/dark. Screenshots inspected side by side for main, calendar, settings, calculation, sound picker, About, widget and hover card.
- Actual Windows navigation to converter and Adhkar pages passed and screenshots were inspected.
- Standalone source build requires no original binary or source-generation step.
- Bundled .NET, Windows App SDK and VC runtime verified; native app launches without shared Windows App Runtime.
- Portable packaging, Program Files installation with administrator elevation, installed launch, running-app upgrade, Start menu/desktop shortcuts, uninstall and settings retention passed.

## Intentional differences and fixes

- Repository, About and updater URLs point to StarsDust01/fluent-prayer-times.
- The generated application PRI is explicitly copied during publish, preventing a startup resource crash.
- Installer upgrades signal the app's existing Quit event before replacing running files. The recovered original installer failed that test with files in use.
- Original README wording recovered from historical complete writes and later recorded edits. Lost screenshots are replaced only by fresh screenshots supplied by the owner. The original app icon and audio are preserved.

## Scope limits

This is not a proof of every network response, every Windows version or every interactive path. Live public updater checks passed against the published v1.3.6 installer: asset selection, full download, progress to 100%, size/header/checksum verification, cancellation cleanup, wrong-checksum rejection and untrusted-host rejection. The download UI was inspected at "Downloaded and verified", with Install now/Later controls.

## Evidence

- Native differential/self-tests and light/dark screens: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40759438806
- Original packaging plus upgrade fix: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40762673696
- Actual converter/Adhkar navigation: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40764185902
- Standalone source build and packaging: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40764144288

- Public updater and release download verification: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40767130541
- Post-cleanup native build/packaging: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40765200522

The release uses the compatible `FluentPrayerTimes-v1.3.6-installer-win-x64.exe` name. Its bytes are self-contained. The identical longer-name installer asset was removed with the owner's approval after live updater verification.

## Final installer correction, same 1.3.6 version

Owner requested Program Files, plain names and a desktop-shortcut checkbox. The installer now uses `C:\Program Files\FluentPrayerTimes`, HKLM installed-apps registration, common Start menu shortcut and optional common desktop shortcut (checked by default, matching original creation). The updater explicitly requests elevation for the installer.

Tested upgrading the formerly published per-user 1.3.6: old folder, old shortcut names and old registry entry removed; settings retained. New installed app launches, running-app upgrade succeeds, uninstall retains settings. Actual Additional Tasks, installed shortcut and Programs and Features screenshots inspected.

- Final candidate build/migration/updater checks: https://github.com/StarsDust01/fluent-prayer-times/actions/runs/40773809325
- Existing v1.3.6 installer and portable assets replaced only after those checks, per owner instruction. No new tag/release.
