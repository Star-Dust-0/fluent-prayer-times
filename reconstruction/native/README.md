# Native editable WinUI project (WIP)

This project uses recovered C# and reconstructed editable XAML. `prepare-source.py` removes the original compiler-generated connectors from the recovered classes; the Windows XAML compiler regenerates them from the editable XAML, including event bindings recovered from the original connectors.

Use Windows with .NET 8 and the Windows SDK:

```powershell
python prepare-source.py
dotnet publish -c Release -r win-x64 -p:Platform=x64 -o publish
```

Icons, audio and other app assets need to be copied from the original backup. The next verification stage compares this native rebuild against the original application on a Windows runner. Until then this is unverified work, not a replacement release.
