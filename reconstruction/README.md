# Fluent Prayer Times 1.3.6 reconstruction (WIP)

This is a source reconstruction of the shipped 1.3.6 assembly, not the original source. The recovered historical source at the repository root stays unchanged.

## State

- The app assembly compiles with .NET SDK 8.0.427, using reference assemblies from the original self-contained backup.
- The unmodified assemblies cannot run on Linux: their Windows bootstrap module initializer fails.
- For isolated managed tests only, test copies of both assemblies had that initializer replaced with return. 22,250 differential cases pass (zero exceptions). They cover solar calculations, countdown formatting, all-day prayer/iqama states and iqama settings bounds. This does not verify native bootstrap, UI or full app parity.
- The isolation script is in `tests/isolate-bootstrap`. It must never patch the shipping assembly.
- Native Windows launch, XAML resources and interactive behavior are not verified yet. Do not install or distribute this build as a replacement.
- The original compiled XAML is in `resources.pri`. Editable XAML reconstruction remains pending.

## Build

Extract the original self-contained backup to a local directory. Do not commit it.

```sh
dotnet build src/FluentPrayerTimes.csproj -p:OriginalAppDir=/absolute/path/to/FluentPrayerTimes-SelfContained
```

The app's dependencies are .NET 8, Windows App SDK 1.6.250108002, Windows SDK .NET Ref 10.0.19041.57 and WebView2 projection 1.0.2651.64, from the shipped dependency manifest.

## Differential checks

```sh
dotnet run --project tests/Parity.csproj -- /absolute/path/to/original/FluentPrayerTimes.dll src/bin/Debug/netcoreapp8.0/FluentPrayerTimes.dll
```

The check program loads the assemblies in separate contexts and invokes both versions with the same inputs. Any unexpected exception fails the check.

Decompiler repairs so far: remove a compiler-only RefSafetyRules attribute; replace an untyped null tuple element with an explicit type; remove an invalid generated named argument for a local function. No intended behavior changes.

brought to you by Instinct
