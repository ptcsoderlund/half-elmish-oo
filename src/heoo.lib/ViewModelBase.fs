﻿module heoo.lib.ViewModelBase

open System.Collections.Concurrent
open System.ComponentModel
open System.Runtime.CompilerServices

///Create a ViewModel class by inheriting this one.
/// use getPropertyValue in getters and messageDispatch in setters.
/// Connect the update function to a elmish program.
type T<'ModelT, 'MessageT>(initialModel: 'ModelT) =
    //INotifyPropertychanged
    let propEv = Event<_, _>()

    let propertyGetters =
        ConcurrentDictionary<string, 'ModelT -> obj>()

    let propertyErrors =
        ConcurrentDictionary<string, 'ModelT -> string>()

    let mutable currentModel = initialModel

    member _.getPropertyValue<'T>(getFunc: 'ModelT -> 'T, [<CallerMemberName>] ?propertyName) =
        match propertyName with
        | Some pName ->
            let wrapper =
                fun modelX -> getFunc (modelX) :> obj

            propertyGetters.TryAdd(pName, wrapper) |> ignore
            getFunc (currentModel)
        | _ -> failwith "propertyName cant be null when setting values"

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

        let oldModel = currentModel
        currentModel <- newModel
        //Run all functions in all properties and trigger notification when changed.
        //Somewhat guarded against null values, but plz avoid them at all costs.
        propertyGetters.Keys
        |> Seq.iter (fun key ->
            let getfunc = propertyGetters[key]

            let nullAsNone (item: obj) = if item = null then None else Some item

            let a = getfunc (oldModel) |> nullAsNone
            let b = getfunc (currentModel) |> nullAsNone

            if
                match (a, b) with
                | (Some x, Some y) -> x.Equals(y) |> not
                | _ -> a = b |> not
            then
                propEv.Trigger(this, PropertyChangedEventArgs(key)))

    interface INotifyPropertyChanged with

        [<CLIEvent>]
        override this.PropertyChanged = propEv.Publish

    interface IDataErrorInfo with
        //I dont know what this is for?
        member this.Error = System.String.Empty

        member this.Item
            with get columnName =
                if propertyErrors.ContainsKey(columnName) then
                    propertyErrors[columnName](currentModel)
                else
                    System.String.Empty