// Learn more about F# at http://fsharp.net

// Learn more about F# at http://fsharp.net

module Module1

    open Microsoft.FSharp.Quotations
    open Linq.QuotationEvaluation
    open System

    let addE = <@ fun a b -> a + b @>

    let answ = addE.Eval() 4 5

    System.Console.WriteLine( answ.ToString() )

    let ser = addE.Compile()

   

    let lol = addE.ToLinqExpression()

    System.Console.ReadKey() |> ignore
    