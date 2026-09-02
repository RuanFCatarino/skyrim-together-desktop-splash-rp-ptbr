# Building the dependency add-on

Requirements: Windows 10/11 and the .NET Framework 4.x SDK compiler.

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:DesktopSplashTogetherLauncher.exe DesktopSplashTogetherLauncher.cs
```

The compiled launcher keeps assembly version `1.2.0.0`. It requires the original
Desktop Splash Screen files at runtime and does not contain or modify them.

