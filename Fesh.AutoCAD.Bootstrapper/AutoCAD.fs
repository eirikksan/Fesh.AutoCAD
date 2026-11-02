namespace Fesh.AutoCAD.Bootstrapper

open System
open System.IO
open System.Xml.Linq

module AutoCAD =

    let bundleFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Autodesk", "ApplicationPlugins", "Fesh.AutoCAD.bundle")

    let getXml (dllPath: string) (version: string) =
        let attr name (value: string) = XAttribute(XName.Get name, value)
        let element name (content: obj array) = XElement(XName.Get name, content)
        XDocument(
            XDeclaration("1.0", "utf-8", null),
            [| element "ApplicationPackage" [|
                attr "SchemaVersion" "1.0"
                attr "AppVersion" version
                attr "Name" "Fesh.AutoCAD"
                attr "Description" "F# editor and scripting host for AutoCAD 2027"
                attr "Author" "Goswin Rothenthal"
                attr "ProductCode" "{5B83A504-FF2D-4BAE-97CB-0DEB1046A5C2}"
                attr "UpgradeCode" "{5B83A504-FF2D-4BAE-97CB-0DEB1046A5C2}"
                element "CompanyDetails" [|
                    attr "Name" "Goswin Rothenthal"
                    attr "Url" "https://github.com/goswinr/Fesh.AutoCAD"
                |]
                element "Components" [|
                    element "RuntimeRequirements" [|
                        attr "OS" "Win64"
                        attr "Platform" "AutoCAD*"
                        attr "SeriesMin" "R26.0"
                        attr "SeriesMax" "R26.0"
                    |]
                    element "ComponentEntry" [|
                        attr "AppName" "Fesh.AutoCAD"
                        attr "AppType" ".Net"
                        attr "ModuleName" dllPath
                        attr "LoadOnAutoCADStartup" "False"
                        attr "LoadOnCommandInvocation" "True"
                        element "Commands" [|
                            attr "GroupName" "FeshAutoCAD"
                            element "Command" [|
                                attr "Global" "FESH"
                                attr "Local" "FESH"
                            |]
                        |]
                    |]
                |]
            |] :> obj |])

    let installFrom (sourceDirectory: string) (destinationBundle: string) =
        for required in [ "Fesh.AutoCAD.dll"; "Fesh.dll"; "FSharp.Core.dll"; "SyntaxHighlightingFSharp.xshd" ] do
            let path = Path.Combine(sourceDirectory, required)
            if not (File.Exists path) then
                raise (FileNotFoundException($"Installation is incomplete. Build or publish the bootstrapper again. Missing: {path}", path))

        let payloadName = "install-" + Guid.NewGuid().ToString("N")
        let payloadDirectory = Path.Combine(destinationBundle, "Contents", payloadName)
        let manifestPath = Path.Combine(destinationBundle, "PackageContents.xml")
        let temporaryManifest = Path.Combine(destinationBundle, payloadName + ".xml")
        let sourceFiles = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories)
        try
            for source in sourceFiles do
                let destination = Path.Combine(payloadDirectory, Path.GetRelativePath(sourceDirectory, source))
                Directory.CreateDirectory(Path.GetDirectoryName destination) |> ignore
                File.Copy(source, destination)

            let version = Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
            let xml = getXml ($"./Contents/{payloadName}/Fesh.AutoCAD.dll") version
            xml.Save temporaryManifest
            File.Move(temporaryManifest, manifestPath, true)
            manifestPath
        with _ ->
            if File.Exists temporaryManifest then File.Delete temporaryManifest
            if Directory.Exists payloadDirectory then Directory.Delete(payloadDirectory, true)
            reraise()

    let unregisterFrom (destinationBundle: string) =
        let manifestPath = Path.Combine(destinationBundle, "PackageContents.xml")
        if File.Exists manifestPath then File.Delete manifestPath

    let ensureAutoCADClosed() =
        let processes = Diagnostics.Process.GetProcessesByName("acad")
        let isRunning = processes.Length > 0
        for acadProcess in processes do acadProcess.Dispose()
        if isRunning then
            invalidOp "Close all AutoCAD windows, then run the bootstrapper again. No installation changes were made."

    let register() =
        ensureAutoCADClosed()
        installFrom AppContext.BaseDirectory bundleFolder

    let unregister() =
        unregisterFrom bundleFolder



