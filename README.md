# half-elmish-oo

This library is kind of experimental.
I want to create elmish program loops (message processing loops) with a simple callback.

## Example
### Step1 - Design your program
```fsharp
open heoo.lib

//Elmish programs need a model a message and update function.

type Model = { Count: int; SomeText:string }
type Message = 
    | Increase 
    | Decrease
    | Reset
    | SetText of string

let Update (msg:Message) (model:Model) =
    match msg with
    | Increment -> { model with Count = model.Count + 1 }
    | Decrement -> { model with Count = model.Count - 1 }
    | Reset -> { model with Count = 0 }
    | SetText text -> { model with SomeText = text }
   
```
### Step2 - Create your viewmodel 
```fsharp
type MyVm(initialModel,messageCallback) =
    inherit ViewModelBase.T<Model,Message>(initialModel)
    
    //Remember that this is async.
    //Wait for InotifyPropertyChanged until getter is properly updated.
    member this.GetSetSomeText
        with get() = this.getPropertyValue(fun m -> m.SomeText)
        and set v = v |> SetText |> messageCallback 
    
    member this.GetCounter
        with get() = this.getPropertyValue(fun m -> m.Count)
        
    member this.IncreaseCmd = 
        CommandBase.AlwaysExecutableCommand(fun _ -> messageCallback Increase)
    member this.DecreaseCmd = 
        CommandBase.AlwaysExecutableCommand(fun _ -> messageCallback Decrease)
    member this.ResetCmd
        with get() = 
            this.getPropertyValue(fun m ->
                let canExecute = fun _ -> m.Count <> 0//already reset
                let execute = fun _ -> messageCallback Reset
            )     
```

### Step3 - Instantiate your program loop and viewmodel
```fsharp

let initialModel = { Count = 0; SomeText = "Hello World" }
let program = ElmishProgramAsync.T(initialModel,Update)
let viewModelInstance = MyVm(initialModel,program.PostMessage)

//Remember to wire your programs OnModelUpdated action to your viewmodel
//It might be a good idea to not write this here but to move it into your gui application.
//Where you actually bind viewmodel to view.
program.OnModelUpdated <- 
    Action<Model>(
        fun m ->
          //if this is a gui application, thread synchronization is (usually) needed.
          //This might be a good place for it to happen.
          //example: Dispatcher.Invoke(viewMModelInstance.updateModel m)
          viewModelInstance.updateModel m
    )
```

### Step4 - Consume your viewmodel

Consume your viewmodel in your application (wpf, Winforms, Avalonia, MAUI.net,Uno, Unity3D etc)
As you would with any other INotifyPropertyChanged, IDataErrorInfo, ICommand implementation.
