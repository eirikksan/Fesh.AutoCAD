namespace Fesh.AutoCAD.Bootstrapper

open System
open System.Windows
open Velopack
open System.Windows.Media
open AvalonLog

module Main =

    let window(log:AvalonLog) =
        let win = Window()
        win.Content <- log
        win.Title <- "Fesh.AutoCAD.Bootstrapper"
        win.Width <- 800.0
        win.Height <- 600.0
        win


    let greet (log:AvalonLog) =
        log.printfnBrush Brushes.Blue "Fesh.AutoCAD installer | AutoCAD 2027 | .NET 10"
        log.printfnBrush Brushes.Blue "Per-user installation. Administrator rights are not required."


    let velo(log:AvalonLog) =
        let vpk = VelopackApp.Build()

        vpk.SetAutoApplyOnStartup(false)
            |> ignore // to not install updates if they are downloaded

        vpk.OnBeforeUninstallFastCallback(fun _ ->
            AutoCAD.unregister()
            ) |> ignore

        vpk.OnRestarted(                  fun a ->
            log.printfnBrush Brushes.DarkGreen $"Fesh.AutoCAD was updated to {a.Major}.{a.Minor}.{a.Patch} !"
            ) |> ignore

        vpk.Run() //https://docs.velopack.io/getting-started/csharp


    [< EntryPoint >]
    [< STAThread >]
    let main (args: string []) : int =
        try
            let app = Application()
            let log = AvalonLog()
            log.ShowLineNumbers <- false
            Console.SetOut   (log.GetTextWriter(Brushes.DarkGray))
            Console.SetError (log.GetTextWriter(Brushes.Red))
            greet(log)
            velo(log) // this might kill the app early !

            let win = window(log)
            let mutable exitCode = 0
            win.ContentRendered.Add(fun _ ->
                try
                    match args with
                    | [| "--uninstall" |] ->
                        AutoCAD.ensureAutoCADClosed()
                        AutoCAD.unregister()
                        log.printfnBrush Brushes.DarkGreen "Fesh.AutoCAD registration was removed. It will no longer load in AutoCAD."
                        log.printfnBrush Brushes.Black $"Installed files remain in {AutoCAD.bundleFolder} and can now be deleted."
                    | [||] | [| "--install" |] ->
                        let manifest = AutoCAD.register()
                        log.printfnBrush Brushes.DarkGreen $"Fesh.AutoCAD was installed successfully.\r\n{manifest}"
                        log.printfnBrush Brushes.Black "Start AutoCAD 2027, open a drawing, and run FESH."
                    | _ ->
                        invalidArg "args" "Usage: Fesh.AutoCAD.Bootstrapper.exe [--install | --uninstall]"
                    log.printfnBrush Brushes.Black "You can close this window now."
                with error ->
                    exitCode <- 1
                    log.printfnBrush Brushes.Red $"Installation failed:\r\n{error.Message}")
            app.Run(win) |> ignore
            exitCode

        with e ->
            MessageBox.Show($"Fesh.AutoCAD.Bootstrapper Start Up Error:\r\n{e}", "Fesh.AutoCAD installation failed", MessageBoxButton.OK, MessageBoxImage.Error) |> ignore
            1



