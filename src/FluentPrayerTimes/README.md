# Fluent Prayer Times source

Native WinUI 3 app for Windows x64. Build with Visual Studio MSBuild and .NET 8:

```powershell
msbuild FluentPrayerTimes.csproj /restore /t:Publish /p:Configuration=Release /p:RuntimeIdentifier=win-x64 /p:Platform=x64 /p:PublishDir=../../publish/
```

The source tree is standalone. It does not need the original application binary, decompilation tools or a generation step. Audio and icons are included. The project copies its generated application PRI into the publish directory.
