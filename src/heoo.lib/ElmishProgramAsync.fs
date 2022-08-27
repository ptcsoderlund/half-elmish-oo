module heoo.lib.ElmishProgramAsync

open System
//For managing the program.
type private programMessage<'ModelT,'MessageT> =
    | AddSubscriber of ('ModelT -> unit)
    | RemoveSubscriber of ('ModelT -> unit)
    | ProgramInstruction of 'MessageT
    | Dispose
   
//Create a message loop for the program.
type T<'ModelT,'MessageT>(initialModel:'ModelT,updateFun:'ModelT -> 'MessageT -> 'ModelT) =
    let mutable subscribingFuncs:('ModelT -> unit) list= []
    let mutable _isDisposed:bool = false
    
    let rec programLoop (inbox:MailboxProcessor<programMessage<'ModelT,'MessageT>>) model = async {
        let! message = inbox.Receive()
        match message with
        | AddSubscriber modelFunc -> subscribingFuncs <- subscribingFuncs@[modelFunc]
        | RemoveSubscriber modelTFunc -> subscribingFuncs <- subscribingFuncs |> List.filter(fun x -> LanguagePrimitives.PhysicalEquality x modelTFunc)
        | ProgramInstruction messageT ->
            let newModel = updateFun model messageT
            subscribingFuncs
                |> List.iter(fun x -> x newModel)
            return! programLoop inbox newModel
        | Dispose -> _isDisposed <- true
        if _isDisposed |> not then
            return! programLoop inbox model
    }
    let mBoxProcessor = MailboxProcessor.Start(fun inbox -> programLoop inbox initialModel)
    
    member _.PostMessage mess = mBoxProcessor.Post(ProgramInstruction mess)
    ///Subscribe to the program.
    ///The function will be called whenever the program updates. 
    member _.Subscribe (listenerFunction:'ModelT -> unit) = mBoxProcessor.Post(AddSubscriber listenerFunction)
    ///Unsubscribe from the program, by passing in listenerfunction reference (always a reference?). 
    member _.UnSubscribe (listenerFunction:'ModelT->unit) = mBoxProcessor.Post(RemoveSubscriber listenerFunction)
    interface IDisposable with
        member this.Dispose() = mBoxProcessor.Post(Dispose) 
