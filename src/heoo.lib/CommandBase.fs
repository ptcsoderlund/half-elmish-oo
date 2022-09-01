module heoo.lib.CommandBase

open System.Windows.Input

type CanExecuteType = obj option -> bool
type ExecuteType = obj option -> unit
///ICommand wrapper for buttons and stuff that needs to know if a command can execute.
type T(canExecute:CanExecuteType,execute:ExecuteType) =
    let canExecuteChangedEv = Event<_,_>()
    member this.AsICommand
        with get() = this :> ICommand
    interface ICommand with
        member this.CanExecute(parameter) =
            if parameter = null then
                canExecute(None)
            else canExecute <| Some parameter
        

        member this.Execute(parameter) =
            if parameter = null then
                execute None
            else execute (Some parameter)
            
        [<CLIEvent>]
        member this.CanExecuteChanged = canExecuteChangedEv.Publish
    
type AlwaysExecutableCommand(execute:ExecuteType) =
    inherit T((fun _ -> true),execute)


