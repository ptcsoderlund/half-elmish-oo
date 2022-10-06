# half-elmish-oo

I want to create elmish program loops (message processing loops) with a simple callback.


## Example
### Step1 - Design your program
```F#
open heoo.lib
open System

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
   
```
### Step2 - Create your viewmodel 
```F#
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
    member this.ResetCmd
        with get() = 
            this.getPropertyValue(fun m ->
                let canExecute = fun _ -> m.Count <> 0//already reset
                let execute = fun _ -> messageCallback Reset
                CommandBase.T(canExecute,execute)
            )
    member this.GetAllErrorMessages
        //ignore the keys (propertynames) and just get the values in an array
        with get():string array = this.getPropertyValue(
            fun model ->
                //Errors are stored as key,value pairs.
                //Where value is a function that returns a string from given model.
                this.GetErrorsArray()
                |> Array.map(fun (_,errorFunc) -> errorFunc model)
            )    
```

### Step3 - Instantiate your program loop and viewmodel
```F#

let initialModel = { Count = 0; SomeText = "Hello World" }
let program = new ElmishProgramAsync.T<Model,Message>(initialModel,Update)
//WARNING!, once you call (IDisposable)Dispose() on the program loop, you can't use it anymore.
//like this: program.AsIDisposable().Dispose()
let viewModelInstance = MyVm(initialModel,program.PostMessage)

```
### Step3.1 - wire OnModelUpdated to viewmodel
```F#
//Remember to wire your programs OnModelUpdated action to your viewmodel
//It might be a good idea to not write this here and move it into your gui application instead.
program.OnModelUpdated <- 
    Action<Model>(
        fun m ->
          //if this is a gui application, thread synchronization is (usually) needed.
          //This might be a good place for it to happen.
          //...
          //example: Dispatcher.Invoke(viewMModelInstance.updateModel m)
          viewModelInstance.updateModel m
    )
    |> Some
```

### Step4 - Consume your viewmodel

Consume your viewmodel in your application (wpf, Winforms, Avalonia, MAUI.net,Uno, Unity3D etc)
As you would with any other INotifyPropertyChanged,INotifyDataErrorInfo, IDataErrorInfo, ICommand implementation.

### Additional comments

It should  be no problem to create more elmishprograms to avoid the whole monolithic approach.
It's also possible to have multiple viewmodels from one program.
I'm trying to leave the door open for more advanced scenarios.  
Where you as a consumer is in control.  

Don't mistake my absence for abandonment.  
This library is complete and stable and will stand the test of time.  
heoo.lib isnt about running fast or optimizing, its only a layer between logic and ui.
I use it in my own products which are planned to run for 10+ years.