﻿module heoo.lib.ViewModelBase

open System
open System.Collections.Concurrent
open System.ComponentModel
open System.Runtime.CompilerServices

///The idea here is to inherit this class for a viewmodel class.
/// Use this.getPropertyValue always, in getters.
/// Wire ElmishProgramAsync.OnModelUpdated into T.updateModel.
/// Also use ElmishProgramAsync.PostMessage in setters.
[<AbstractClass>]
type T<'ModelT>(initialModel: 'ModelT) =
    //INotifyPropertychanged
    let propEv = Event<_, _>()
    let errorEv = Event<_, _>()
    //Last model update values.
    //Need to store them so we can compare and trigger property changed events.
    let lastUpdatedPropertyValues = ConcurrentDictionary<string, obj>()
    let propertyGetters =
        ConcurrentDictionary<string, 'ModelT -> obj>()

    let propertyErrors =
        ConcurrentDictionary<string, 'ModelT -> string>()

    member val private currentModel = initialModel with get,set 

    member this.getPropertyValue<'T>(getFunc: 'ModelT -> 'T, [<CallerMemberName>] ?propertyName) =
        match propertyName with
        | Some pName ->
            let wrapper =
                fun modelX -> getFunc (modelX) :> obj
            if propertyGetters.ContainsKey pName |> not then
                propertyGetters.TryAdd(pName, wrapper) |> ignore
            getFunc (this.currentModel)
        | _ -> failwith "propertyName cant be null when setting values"
        
    //Creates a command and a property which triggers propertyChanged on the "canExecute" function.
    //Signature of CanExecute is CommandParameter -> Model -> bool
    member this.getCommandBaseT<'T>(canExecute:Object option->'ModelT -> bool,execute: Object option -> unit,[<CallerMemberName>] ?propertyName:string) :CommandBase.T =
        let propName =
            match propertyName with
            | Some v -> v
            | _ -> failwith "propertyName cant be null when setting values"
        let _canExecute = fun (o:obj option) ->
            canExecute o 
            |> fun x ->  this.getPropertyValue(x,propName)
        CommandBase.T(_canExecute,execute)
        
    member _.getPropertyError(getFunc: 'ModelT -> string, [<CallerMemberName>] ?propertyName) =
        match propertyName with
        | Some pName -> propertyErrors.TryAdd(pName, getFunc) |> ignore
        | _ -> failwith "propertyName cant be null when setting errors"

    abstract member updateModel: 'ModelT -> unit
    //Get all errors as (propertyName, errorMessage) tuple.
    member this.GetErrorsArray() =
        propertyErrors.ToArray()
        |> Array.map (fun x -> (x.Key, x.Value))

    default this.updateModel(newModel: 'ModelT) =
        if (newModel :> obj) = null then
            failwith "null is toxic, dont pass it in"

        this.currentModel <- newModel
        //Run all functions in all properties and trigger notification when changed.
        //Somewhat guarded against null values, but plz avoid them at all costs.
        propertyGetters.Keys
        |> Seq.iter (fun key ->
            let getfunc = propertyGetters.[key]
            let lastValueExists = lastUpdatedPropertyValues.ContainsKey(key)
            let nullAsNone (item: obj) = if item = null then None else Some item

            let newValue_op = getfunc (this.currentModel) |> nullAsNone
            
            //We trigger event if last value doesnt exist, or if it does and its different.
            let triggerEvent =
                match (lastValueExists, newValue_op) with
                | (true, Some newValue) ->
                    if LanguagePrimitives.GenericEquality lastUpdatedPropertyValues.[key] newValue |> not then
                        lastUpdatedPropertyValues[key] <- newValue
                        true 
                    else false 
                | (false, Some newValue) -> lastUpdatedPropertyValues.TryAdd(key, newValue)
                | (_, None) -> false//We dont show support to null
            if
                triggerEvent
            then
                //Make sense to trigger error event here, error text could have changed.
                //Might optimize by checking if it has actually changed, which is suspect can cause overhead and defeat its purpose.
                if propertyErrors.ContainsKey(key) then
                    errorEv.Trigger(this, DataErrorsChangedEventArgs(key))
                
                propEv.Trigger(this, PropertyChangedEventArgs(key)))
        
    member this.AsINotifyPropertyChanged =
        this :> INotifyPropertyChanged

    member this.AsIDataErrorInfo =
        this :> IDataErrorInfo

    member this.AsINotifyDataErrorInfo =
        this :> INotifyDataErrorInfo

    interface INotifyPropertyChanged with

        [<CLIEvent>]
        override this.PropertyChanged = propEv.Publish

    interface IDataErrorInfo with
        //I dont know what this is for?
        member this.Error = System.String.Empty

        member this.Item
            with get columnName =
                if propertyErrors.ContainsKey(columnName) then
                    propertyErrors.[columnName] (this.currentModel)
                else
                    System.String.Empty

    interface INotifyDataErrorInfo with
        member this.HasErrors =
            propertyErrors.Values
            |> Seq.filter (fun v -> v (this.currentModel) <> System.String.Empty)
            |> Seq.isEmpty
            |> not

        member this.GetErrors(columnName) =
            if (propertyErrors.ContainsKey(columnName)
                && propertyErrors.[columnName] (this.currentModel) <> "") then
                [ propertyErrors.[columnName] (this.currentModel) ]
            else
                []

        [<CLIEvent>]
        member this.ErrorsChanged = errorEv.Publish

    override this.Equals(other) =
        match other with
        | :? T<'ModelT> as otherT ->
            let otherModel = otherT.currentModel :> obj
            let thisModel = this.currentModel :> obj
            let result =  thisModel.Equals(otherModel)
            result
        | otherO -> (this.currentModel :> obj).Equals(otherO)
    override this.GetHashCode() = (this.currentModel :> obj).GetHashCode()