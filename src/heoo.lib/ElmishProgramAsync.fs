module heoo.lib.ElmishProgramAsync

open System
//For managing the program.
type private programMessage<'ModelT,'MessageT> =
    | ProgramInstruction of 'MessageT
    | Dispose
    
type ModelUpdateTrigger =
    | AfterEachMessage
    | AfterAllMessages//Recommended, when using viewmodels as consumers.
    
///Create a message loop program
///Remember to set OnModelUpdated to subscribe to model updates (and maybe do thread synch there?).
/// Message processing stops when the program is disposed.
type T<'ModelT,'MessageT>(initialModel:'ModelT,updateFun:'ModelT -> 'MessageT -> 'ModelT,?whenToUpdateModel:ModelUpdateTrigger) =
    let whenToUpdateModel = defaultArg whenToUpdateModel ModelUpdateTrigger.AfterAllMessages 
    let mutable onModelUpdated: Action<'ModelT> option = None
    let mutable _isDisposed:bool = false
    let mutable _model = initialModel
    let programLoop (inbox:MailboxProcessor<programMessage<'ModelT,'MessageT>>) = async {
        while _isDisposed |> not do
            let! message = inbox.Receive()
            match message with
            | ProgramInstruction messageT ->
                _model <- updateFun _model messageT
                if inbox.CurrentQueueLength = 0 || whenToUpdateModel = ModelUpdateTrigger.AfterEachMessage then
                    match onModelUpdated with
                    | Some x -> x.Invoke(_model)
                    | None -> ()
            | Dispose -> _isDisposed <- true
    }
    
    let mBoxProcessor = MailboxProcessor.Start(fun inbox -> programLoop inbox)
    member this.AsIDisposable = this :> IDisposable
    member _.PostMessage mess = mBoxProcessor.Post(ProgramInstruction mess)
    member this.OnModelUpdated
        with get() = onModelUpdated
        and set v = onModelUpdated <- v
        
    interface IDisposable with
        member this.Dispose() = mBoxProcessor.Post(Dispose) 
   