module heoo.lib.ViewModelBase

open System.Collections.Concurrent
open System.ComponentModel
open System.Runtime.CompilerServices

///The idea here is to inherit this class for a viewmodel class.
/// Use this.getPropertyValue always, in getters.
/// Wire ElmishProgramAsync.OnModelUpdated into T.updateModel.
/// Also use ElmishProgramAsync.PostMessage in setters.
[<AbstractClass>]
type T<'ModelT >(initialModel: 'ModelT) =
    //INotifyPropertychanged
    let propEv = Event<_, _>()
    let errorEv = Event<_, _>()
    let propertyGetterFuncs =
        ConcurrentDictionary<string, 'ModelT -> obj>()
    let propertyGetterValues = ConcurrentDictionary<string,obj>()
    let propertyErrorsFuncs =
        ConcurrentDictionary<string, 'ModelT -> string>()

    let mutable currentModel = initialModel

    member _.getPropertyValue<'T>(getFunc: 'ModelT -> 'T, [<CallerMemberName>] ?propertyName) =
        match propertyName with
        | Some pName ->
            match propertyGetterValues.TryGetValue pName with
            | true, value -> value :?> 'T
            | false, _ ->
                //this should be the first time run
                let wrapper =
                    fun modelX -> getFunc (modelX) :> obj
                let valueT:'T = getFunc(currentModel) 
                propertyGetterFuncs.TryAdd(pName, wrapper) |> ignore
                propertyGetterValues.TryAdd(pName,valueT :> obj) |> ignore
                valueT
        | _ -> failwith "propertyName cant be null when setting values"

    member _.getPropertyError(getFunc: 'ModelT -> string, [<CallerMemberName>] ?propertyName) =
        match propertyName with
        | Some pName -> propertyErrorsFuncs.TryAdd(pName, getFunc) |> ignore
        | _ -> failwith "propertyName cant be null when setting errors"

    abstract member updateModel: 'ModelT -> unit
    //Get all errors as (propertyName, errorMessage) tuple.
    member this.GetErrorsArray() =
        propertyErrorsFuncs.ToArray()
        |> Array.map (fun x -> (x.Key, x.Value))

    default this.updateModel(newModel: 'ModelT) =
        if (newModel :> obj) = null then
            failwith "null is toxic, dont pass it in"

        currentModel <- newModel
        //Run all functions in all properties and trigger notification when changed.
        //Somewhat guarded against null values, but plz avoid them at all costs.
        propertyGetterFuncs.Keys
        |> Seq.iter (fun key ->
            let getfunc = propertyGetterFuncs.[key]
            let currentValue = propertyGetterValues.[key]
            let nullAsNone (item: obj) = if item = null then None else Some item
            let newValue = getfunc newModel 

            let currentValueSafe = currentValue |> nullAsNone
            let newValueSafe = newValue |> nullAsNone
            if
                match (currentValueSafe, newValueSafe) with
                | (Some x, Some y) -> x.Equals(y) |> not
                | _ -> currentValueSafe.Equals(newValueSafe) |> not
            then
                //Make sense to trigger error event here, error text could have changed.
                //Might optimize by checking if it has actually changed, which is suspect can cause overhead and defeat its purpose.
                if propertyErrorsFuncs.ContainsKey(key) then
                    errorEv.Trigger(this, DataErrorsChangedEventArgs(key))
                propertyGetterValues.[key] <- newValue
                propEv.Trigger(this, PropertyChangedEventArgs(key)))
    member this.AsINotifyPropertyChanged
        with get() = this :> INotifyPropertyChanged
    member this.AsIDataErrorInfo
        with get() = this :> IDataErrorInfo
        
    member this.AsINotifyDataErrorInfo
        with get() = this :> INotifyDataErrorInfo
    interface INotifyPropertyChanged with

        [<CLIEvent>]
        override this.PropertyChanged = propEv.Publish

    interface IDataErrorInfo with
        //I dont know what this is for?
        member this.Error = System.String.Empty

        member this.Item
            with get columnName =
                if propertyErrorsFuncs.ContainsKey(columnName) then
                    propertyErrorsFuncs.[columnName](currentModel)
                else
                    System.String.Empty
    
    interface INotifyDataErrorInfo with
        member this.HasErrors
            with get() = propertyErrorsFuncs.Values |> Seq.filter(fun v -> v(currentModel) <> System.String.Empty) |> Seq.isEmpty |> not 

        member this.GetErrors(columnName) =
            if (propertyErrorsFuncs.ContainsKey(columnName) && propertyErrorsFuncs.[columnName](currentModel) <> "" ) then
                [ propertyErrorsFuncs.[columnName](currentModel) ]
            else
                []
        [<CLIEvent>]
        member this.ErrorsChanged = errorEv.Publish 