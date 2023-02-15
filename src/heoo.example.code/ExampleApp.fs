module heoo.example.code.ExampleApp

open System
open heoo.lib

//Lets just make a counter app for demoing purpose
//Elmish programs need a model a message and update function.

//Step 1
//Elmish programs need a model a message and update function.

type Model = { Count: int; SomeText:string }
type Message = 
    | Increase 
    | Decrease
    | Reset
    | SetText of string

let Update  (model:Model) (msg:Message) =
    match msg with
    | Increase -> { model with Count = model.Count + 1 }
    | Decrease -> { model with Count = model.Count - 1 }
    | Reset -> { model with Count = 0 }
    | SetText text -> { model with SomeText = text }
   
//Step 2
type MyVm(initialModel,messageCallback) =
    inherit ViewModelBase.T<Model>(initialModel)
    
    //Remember that this is async.
    //Wait for InotifyPropertyChanged until getter is properly updated.
    member this.GetSetSomeText
        with get() =
            this.getPropertyError(
                fun m ->
                    match m.SomeText with
                    | "" -> "SomeText cannot be empty"
                    | _ -> ""
                    )//empty string is no error
            this.getPropertyValue(fun m -> m.SomeText)
        and set v = v |> SetText |> messageCallback 
    
    member this.GetCounter
        with get() = this.getPropertyValue(fun m -> m.Count)
        
    member this.IncreaseCmd = 
        CommandBase.AlwaysExecutableCommand(fun _ -> messageCallback Increase)
    member this.DecreaseCmd = 
        CommandBase.AlwaysExecutableCommand(fun _ -> messageCallback Decrease)
    member this.ResetCmd =
           let canExecute = fun _ m -> m.Count <> 0 //commandParameter -> model -> bool
           let execute = fun _ -> messageCallback Reset //commandParameter -> unit
           this.getCommandBaseT(canExecute,execute)
    member this.GetAllErrorMessages
        //ignore the keys (propertynames) and just get the values in an array
        with get():string array = this.getPropertyValue(
            fun model ->
                //Errors are stored as key,value pairs.
                //Where value is a function that returns a string from given model.
                this.GetErrorsArray()
                |> Array.map(fun (_,errorFunc) -> errorFunc model)
            ) 
//Step 3

let initialModel = { Count = 0; SomeText = "Hello World" }
let program = new ElmishProgramAsync.T<Model,Message>(initialModel,Update)
//WARNING!, once you call (IDisposable)Dispose() on the program loop, you can't use it anymore.
//like this: program.AsIDisposable().Dispose()
let viewModelInstance = MyVm(initialModel,program.PostMessage)
//Next step (3.1) is in the view (MainWindow.xaml.cs), since gui thread sync is needed.