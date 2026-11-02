
![Logo](https://raw.githubusercontent.com/goswinr/Fesh.AutoCAD/main/Media/logo128.png)

# Fesh.AutoCAD
[![Build](https://github.com/goswinr/Fesh.AutoCAD/actions/workflows/build.yml/badge.svg?event=push)](https://github.com/goswinr/Fesh.AutoCAD/actions/workflows/build.yml)
![code size](https://img.shields.io/github/languages/code-size/goswinr/Fesh.AutoCAD.svg)
[![license](https://img.shields.io/github/license/goswinr/Fesh.AutoCAD)](LICENSE.md)

Fesh.AutoCAD is an F# scripting editor hosted inside AutoCAD. It is based on [Fesh](https://github.com/goswinr/Fesh).<br>
It has semantic syntax highlighting, auto completion, type info tooltips and more.<br>
The output window supports colored text.

![Screenshot](Media/screen1.png)
The example script in the root folder generates the axes for cladding of the Louvre Abu Dhabi.<br>
See also my talk at <a href="https://www.youtube.com/watch?v=ZY-bvZZZZnE" target="_blank">FSharpConf 2016</a>


## How to install


This build requires **AutoCAD 2027 (64-bit)** and the **.NET 10 Desktop Runtime**.
It does not support AutoCAD 2026 or earlier, which use different .NET runtimes and APIs.

1. Close all AutoCAD windows.
2. Build with `dotnet build Fesh.AutoCAD.slnx -c Release`.
3. Run `Fesh.AutoCAD.Bootstrapper/bin/Release/net10.0-windows/Fesh.AutoCAD.Bootstrapper.exe`.
4. Wait for the installation success message, then close the installer.
5. Start AutoCAD 2027 normally, open a drawing, and enter `FESH`.

No administrator rights are required. The bootstrapper copies the add-in and its
dependencies into `%APPDATA%\Autodesk\ApplicationPlugins\Fesh.AutoCAD.bundle`
and creates `PackageContents.xml`. AutoCAD discovers the bundle on subsequent
starts and loads the editor when you run `FESH`; `NETLOAD` is not required.
The installation is independent of the build folder after setup completes.

If AutoCAD asks whether to load this unsigned local build, approve it only if you
trust the build. Do not disable `SECURELOAD`. AutoCAD must have bundle discovery
enabled through `APPAUTOLOAD`; enterprise policies can restrict per-user plug-ins.

To update a local installation, close AutoCAD, rebuild, and run the bootstrapper
again. A new copy is activated only after all files have been copied successfully.
Previous payload folders are retained to avoid deleting files in use. Local builds
do not provide automatic Velopack updates.

To unregister, close AutoCAD and run the bootstrapper with `--uninstall`.
After closing the bootstrapper, you can delete the `Fesh.AutoCAD.bundle` folder
to remove the retained files. This does not delete editor settings or scripts.

## Installer tests

Run `dotnet fsi Tests/Bootstrapper.Install.Tests.fsx` to test bundle registration,
dependency copying, reinstallation, failure preservation, and unregistration in
a temporary folder without changing your AutoCAD installation.


## Release Notes
For changes in each release see the  [CHANGELOG.md](https://github.com/goswinr/Fesh.AutoCAD/blob/main/CHANGELOG.md)

## License
Fesh is licensed under the [MIT License](https://github.com/goswinr/Fesh.AutoCAD/blob/main/LICENSE.md).
