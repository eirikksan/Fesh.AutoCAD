#load "../Fesh.AutoCAD.Bootstrapper/AutoCAD.fs"

open System
open System.IO
open System.Xml.Linq
open Fesh.AutoCAD.Bootstrapper

let check condition message =
    if not condition then failwith message

let temporaryRoot = Path.Combine(Path.GetTempPath(), "Fesh-install-tests-" + Guid.NewGuid().ToString("N"))
let source = Path.Combine(temporaryRoot, "source & dependencies")
let bundle = Path.Combine(temporaryRoot, "ApplicationPlugins", "Fesh.AutoCAD.bundle")
let manifest = Path.Combine(bundle, "PackageContents.xml")
let name value = XName.Get value

try
    Directory.CreateDirectory(Path.Combine(source, "runtimes", "win-x64", "native")) |> ignore
    for file in [ "Fesh.AutoCAD.dll"; "Fesh.dll"; "FSharp.Core.dll"; "SyntaxHighlightingFSharp.xshd"; "runtimes/win-x64/native/test.dll" ] do
        File.WriteAllText(Path.Combine(source, file), "test payload")

    check (AutoCAD.installFrom source bundle = manifest) "Installer returned the wrong manifest path."
    let xml = XDocument.Load manifest
    let requirements = xml.Descendants(name "RuntimeRequirements") |> Seq.exactlyOne
    check (requirements.Attribute(name "SeriesMin").Value = "R26.0") "Wrong minimum AutoCAD release."
    check (requirements.Attribute(name "SeriesMax").Value = "R26.0") "Unsupported AutoCAD releases are enabled."
    let entry = xml.Descendants(name "ComponentEntry") |> Seq.exactlyOne
    check (entry.Attribute(name "LoadOnCommandInvocation").Value = "True") "Command loading is disabled."
    let command = xml.Descendants(name "Command") |> Seq.exactlyOne
    check (command.Attribute(name "Global").Value = "FESH") "FESH is not registered."
    let installedDll = Path.GetFullPath(Path.Combine(bundle, entry.Attribute(name "ModuleName").Value))
    check (File.Exists installedDll) "Manifest does not point to an installed assembly."
    check (File.Exists(Path.Combine(Path.GetDirectoryName installedDll, "runtimes/win-x64/native/test.dll"))) "Nested dependencies were not copied."

    File.Delete(Path.Combine(source, "Fesh.dll"))
    let beforeFailure = File.ReadAllText manifest
    let mutable rejected = false
    try AutoCAD.installFrom source bundle |> ignore
    with :? FileNotFoundException -> rejected <- true
    check rejected "Incomplete installation was accepted."
    check (File.ReadAllText manifest = beforeFailure) "Failed installation changed the active manifest."

    File.WriteAllText(Path.Combine(source, "Fesh.dll"), "updated payload")
    let payloadsBeforeFailure = Directory.GetDirectories(Path.Combine(bundle, "Contents")) |> Set.ofArray
    do
        use lockedFile = File.Open(Path.Combine(source, "Fesh.dll"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)
        let mutable copyRejected = false
        try AutoCAD.installFrom source bundle |> ignore
        with :? IOException -> copyRejected <- true
        check copyRejected "Copying a locked dependency unexpectedly succeeded."
    check (File.ReadAllText manifest = beforeFailure) "A copy failure replaced the active manifest."
    let payloadsAfterFailure = Directory.GetDirectories(Path.Combine(bundle, "Contents")) |> Set.ofArray
    check (payloadsBeforeFailure = payloadsAfterFailure) "A failed copy left an incomplete payload."

    AutoCAD.installFrom source bundle |> ignore
    check (File.ReadAllText manifest <> beforeFailure) "Reinstallation did not activate a new payload."
    Directory.Delete(source, true)
    let newXml = XDocument.Load manifest
    let newEntry = newXml.Descendants(name "ComponentEntry") |> Seq.exactlyOne
    check (File.Exists(Path.Combine(bundle, newEntry.Attribute(name "ModuleName").Value))) "Installation depends on the source folder."

    let escaped = AutoCAD.getXml "./Contents/a & b/Fesh.AutoCAD.dll" "1.0.0"
    XDocument.Parse(escaped.ToString()) |> ignore
    AutoCAD.unregisterFrom bundle
    AutoCAD.unregisterFrom bundle
    check (not (File.Exists manifest)) "Uninstall did not remove the manifest."
    printfn "PASS: manifest, release gating, command registration, dependency copying, reinstall, failed-install preservation, source independence, XML escaping, and repeated uninstall."
finally
    if Directory.Exists temporaryRoot then Directory.Delete(temporaryRoot, true)