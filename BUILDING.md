# Building

Requirements:

- Windows 10 or 11
- .NET Framework 4.x SDK/compiler

From a Developer Command Prompt, run:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /platform:anycpu /optimize+ /win32icon:SkyrimTogether-user-icon.ico /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:DesktopSplashTogetherLauncher.exe DesktopSplashTogetherLauncher.cs
```

The executable has no third-party managed dependencies and performs no network
access, injection, registry modification, elevation or modification of
`SkyrimTogether.exe`.

