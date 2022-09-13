namespace heoo.example.avalonia

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Threading
open heoo.example.code

type MainWindow () as this = 
    inherit Window ()

    do
        this.InitializeComponent()
        ExampleApp.program.OnModelUpdated <- System.Action<ExampleApp.Model>(this.OnModelUpdated) |> Some
        
    member private this.vm= this.DataContext :?> ExampleApp.MyVm 
    member private this.OnModelUpdated (model: ExampleApp.Model) =
        if Dispatcher.UIThread.CheckAccess() |> not then
            Dispatcher.UIThread.InvokeAsync(System.Action(fun () -> this.OnModelUpdated model),DispatcherPriority.Normal)
            |> ignore
        else
            this.vm.updateModel model
        
    member private this.InitializeComponent() =
#if DEBUG
        this.AttachDevTools()
#endif
        AvaloniaXamlLoader.Load(this)