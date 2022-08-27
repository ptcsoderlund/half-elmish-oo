module heoo.example.code.ExampleApp

open System
open heoo.lib

//Lets just make a counter app for demoing purpose
type Model = { SomeText: string; Counter: int }

type Message =
    | Increase
    | Decrease
    | Reset
    | SetText of string


let Update model message =
    match message with
    | Increase -> { model with Counter = model.Counter + 1 }
    | Decrease -> { model with Counter = model.Counter - 1 }
    | Reset -> { model with Counter = 0 }
    | SetText text -> { model with SomeText = text }

type ExampleVM(initialModel, messageDispatch) =
    inherit ViewModelBase.T<Model, Message>(initialModel)

    member this.GetSetSomeText
        with get() =
            //Not an error if empty
            this.getPropertyError (fun model ->
                match model.SomeText with
                | "" -> "Enter some text"
                | _ -> "")

            this.getPropertyValue (fun model -> model.SomeText)
        and set v = messageDispatch(SetText v)
    member this.GetAllErrorMessages
        with get():string [] =
            this.getPropertyValue(fun model ->
                                  this.GetErrorsArray() |> Array.map(fun (_,mess) -> mess(model)))
    member this.GetCounter =
        this.getPropertyValue (fun model -> model.Counter)

    member this.IncreaseCmd =
        CommandBase.AlwaysExecutableCommand(fun _ -> Increase |> messageDispatch)

    member this.DecreaseCmd =
        CommandBase.AlwaysExecutableCommand(fun _ -> Decrease |> messageDispatch)

    member this.ResetCmd =
        this.getPropertyValue (fun model ->
            CommandBase.T((fun _ -> model.Counter <> 0), (fun _ -> Reset |> messageDispatch)))

let initialModel =
    { SomeText = "Change me"; Counter = 1 }
  
//Remember to bind ElmishProgram.OnNewModel to the vm or vm wont update.
let ElmishProgram = ElmishProgramAsync.T(initialModel,Update)

let SingletonVm = ExampleVM(initialModel,ElmishProgram.PostMessage)