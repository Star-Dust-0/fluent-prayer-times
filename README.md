# Fluent Prayer Times

This branch contains work to reconstruct the shipped Windows app 1.3.6 from its original binary and recovered source. It is not a verified replacement release.

- Historical recovered source stays at the root, unchanged except for this status README.
- `reconstruction/src/` contains the recovered managed implementation, which compiles against the shipped dependencies.
- `reconstruction/xaml/` contains editable XAML recovered from the original resources.
- `reconstruction/native/` regenerates native WinUI source and builds on a Windows runner.
- `reconstruction/tests/` contains differential logic checks. 22,250 cases pass on isolated test copies with Windows bootstrap disabled. That does not verify native startup or UI parity.

Native Windows build and original-versus-rebuilt screenshot verification are in progress. No release should be created from this branch until the full verification report is accepted.

See [reconstruction/README.md](reconstruction/README.md) and [RECOVERY-STATUS.md](RECOVERY-STATUS.md).

brought to you by Instinct

## Updates after account migration

The app checks `StarsDust01/fluent-prayer-times`. The source repository is private. Anonymous GitHub release API requests currently return 404, so the automatic check logs a failed check without showing an update, and the manual check shows the existing error state. This is not evidence that the installed version is current.

No access token is included. A public releases-only repository would allow automatic updates; alternatively, an explicit unavailable notice and a browser link can support signed-in manual downloads. These are choices to agree before changing the update flow further.
