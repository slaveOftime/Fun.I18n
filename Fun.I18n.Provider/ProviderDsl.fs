﻿module rec ProviderDsl

open System.Reflection
open FSharp.Quotations
open ProviderImplementation.ProvidedTypes

type Member =
    | ChildType of System.Type
    | Property of name: string * typ: ErasedType * isStatic: bool * body: (Expr list -> Expr)
    | PropertyGetSet of name: string * typ: ErasedType * isStatic: bool * getBody: (Expr list -> Expr) * setBody: (Expr list -> Expr)
    | Method of name: string * args: (string * ErasedType) list * typ: ErasedType * isStatic: bool * body: (Expr list -> Expr)
    | WithAttrs of m : Member * customAttr : CustomAttributeData []
    | Constructor of args: (string * ErasedType) list * body: (Expr list -> Expr)

type ErasedType =
    | Any
    | Bool
    | Int
    | Float
    | String
    | Array of ErasedType
    | Option of ErasedType
    | Custom of System.Type

let addMembers (t: ProvidedTypeDefinition) members =
    for memb in members do
        let rec getMember attr memb : MemberInfo =
            match memb with
            | WithAttrs (m, attr) -> getMember attr m
            | ChildType t ->
                upcast t
            | Property(name, typ, isStatic, body) ->
                let x = ProvidedProperty(name, makeType typ, isStatic = isStatic, getterCode = body)
                for a in attr do
                    x.AddCustomAttribute(a)
                upcast x
            | PropertyGetSet(name, typ, isStatic, getBody, setBody ) ->
                let x = ProvidedProperty(name, makeType typ, isStatic = isStatic, getterCode = getBody, setterCode = setBody)
                for a in attr do
                    x.AddCustomAttribute(a)
                upcast x
            | Method(name, args, typ, isStatic, body) ->
                let args = args |> List.map (fun (name, t) -> ProvidedParameter(name, makeType t))
                let x = ProvidedMethod(name, args, makeType typ, isStatic = isStatic, invokeCode = body)
                for a in attr do
                    x.AddCustomAttribute(a)
                upcast x
            | Constructor(args, body) ->
                let args = args |> List.map (fun (name, t) -> ProvidedParameter(name, makeType t))
                upcast ProvidedConstructor(args, invokeCode = body)
        t.AddMember(getMember Array.empty memb)

let makeType = function
    | Any -> typeof<obj>
    | Bool -> typeof<bool>
    | Int -> typeof<int>
    | Float -> typeof<float>
    | String -> typeof<string>
    | Array t -> (makeType t).MakeArrayType()
    | Option t -> typedefof<Option<obj>>.MakeGenericType(makeType t)
    | Custom t -> t

let makeCustomType(name: string, members: Member list): System.Type =
    let t = ProvidedTypeDefinition(name, baseType = Some typeof<obj>, hideObjectMethods = true, isErased = true)
    addMembers t members
    upcast t

let makeRootType(assembly: Assembly, nameSpace: string, typeName: string, members: Member list) =
    let root = ProvidedTypeDefinition(assembly, nameSpace, typeName, baseType = Some typeof<obj>, hideObjectMethods = true, isErased = true)
    addMembers root members
    root
