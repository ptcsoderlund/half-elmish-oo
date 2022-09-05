module heoo.example.code.ExampleApp

open System
open heoo.lib

//Lets just make a counter app for demoing purpose
type Model = { SomeText: string;SomeTextTake2:string; SomeTextUnfocus:string; Counter: int }

type Message =
    | Increase
    | Decrease
    | Reset
    | SetText of string
    | SetTextTake2 of string
    | SetTextUnfocus of string


let Update model message =
    match message with
    | Increase -> { model with Counter = model.Counter + 1 }
    | Decrease -> { model with Counter = model.Counter - 1 }
    | Reset -> { model with Counter = 0 }
    | SetText text -> { model with SomeText = text }
    | SetTextUnfocus s -> {model with SomeTextUnfocus=s}
    | SetTextTake2 s -> {model with SomeTextTake2=s}
    
type ExampleVM(initialModel, messageDispatch) =
    inherit ViewModelBase.T<Model, Message>(initialModel)
    let mutable someTextExpected = ""
    member this.GetSomeText
        with get() =
            this.getPropertyValue(
                fun model -> model.SomeText
                )
    member this.GetSetSomeText
        with get() =
            this.getPropertyError (fun model ->
                match model.SomeText with
                | "" -> "Enter some text"
                | _ -> "")
            this.getPropertyValue(
                fun model ->
                    //Apparently wpf spams the getter when used in textbox.
                    //Which means the value wont be updated which sets the textbox text and moves the caret.
                    //Maybe UpdateSourceTrigger=PropertyChanged means if any property changes, the getter is called.
                    if model.SomeText.Equals(someTextExpected) then
                       someTextExpected <- "" 
                    if someTextExpected.Equals "" then
                        model.SomeText
                    else
                        someTextExpected 
                )
        and set v =
            someTextExpected <- v
            messageDispatch(SetText v)
    member this.GetSetSomeText2
        with get() =
            this.getPropertyValue(
                fun model -> model.SomeTextTake2
                )
        and set v =
            messageDispatch(SetTextTake2 v)
            
    member this.GetSetSomeTextUnfocus
        with get() =
            this.getPropertyError(
                fun model ->
                    if model.SomeTextUnfocus.Equals "" then
                        "Enter some text and then unfocus the textbox"
                    else ""
                )
            this.getPropertyValue(
                fun model -> model.SomeTextUnfocus
                )
        and set v =
            v
            |> SetTextUnfocus
            |> messageDispatch
    
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
    { SomeText = "Change me"; SomeTextTake2="Change me 2";SomeTextUnfocus="Change me too" ;Counter = 1 }
  
//Remember to bind ElmishProgram.OnNewModel to the vm or vm wont update.
let ElmishProgram = ElmishProgramAsync.T(initialModel,Update)

let SingletonVm = ExampleVM(initialModel,ElmishProgram.PostMessage)
ElmishProgram.OnModelUpdated <-
    Action<Model>(
        fun m ->
            //This is a good place to invoke on gui thread.
            //example: Dispatcher.Invoke(SingletonVm.updateModel m)
            SingletonVm.updateModel m
        )
    |> Some