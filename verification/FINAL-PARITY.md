# Fluent Prayer Times 1.3.7 verification

The Windows audit passed navigation and controls across all pages, location detection, date conversion, language switching, appearance settings, tray actions, widget controls, startup registration, update handling, and settings-preserving upgrades.

- 128 primary UI assertions and 9 conditional update-control assertions passed.
- 641 calculation, countdown-boundary, and Hijri checks passed. Prayer formulas and Wathkkir rounding are unchanged.
- Scheduler notification gates and all four countdown phases were checked. Main-window, widget, and hover-card screenshots were inspected.
- First-run location and the Detect button were exercised. Saved/manual locations were retained.
- Portable, installer, and full packages include the required runtime files. The installer uses Program Files and offers a desktop shortcut.

Hardware GPS/Windows consent on an individual PC, audible speaker output, and Windows 11 hardware Mica rendering were not certified by this run. Other-language text is machine-translated. Network location can be approximate, especially with a VPN.

[Verified Windows run](https://github.com/Star-Dust-0/fluent-prayer-times/actions/runs/41081273842)
