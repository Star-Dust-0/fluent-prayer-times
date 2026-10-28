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
