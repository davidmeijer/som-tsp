open System
open System.Text

let parseCoords (fn: string) =
    [| use reader = new IO.StreamReader(fn)
       while not reader.EndOfStream do yield reader.ReadLine() |]
    |> (fun arr -> arr.[1 .. arr.Length])
    |> Array.toList
    |> List.map (fun line -> line.Split "," |> Array.toList)
    |> List.map (fun line ->
        match line with
        | _::lon::lat::_ -> float lon, float lat
        | _ -> Exception "Incorrect format input file" |> raise)
    |> List.toArray

let normalize vals =
    let maxVal = Array.reduce max vals
    let minVal = Array.reduce min vals 
    vals |> Array.map (fun v -> (v - minVal) / (maxVal - minVal))
    
let normalizeCoords coords =
    let xCoords = coords |> Array.map (fun (x, _) -> x) |> normalize
    let yCoords = coords |> Array.map (fun (_, y) -> y) |> normalize
    Array.zip xCoords yCoords

let getRandomItem (arr: array<'T>) =
    let rnd = Random()
    Array.item (rnd.Next(arr.Length)) arr

let generateNetwork size =
    let distr = [| 0.0 .. 1.0 / 10000.0 .. 1.0 |]
    let xCoords = [| for _ in [ 1 .. size ] do getRandomItem distr |]
    let yCoords = [| for _ in [ 1 .. size ] do getRandomItem distr |]
    Array.zip xCoords yCoords
    
let distance (xThis, yThis) (xThat, yThat) =
    sqrt(((xThat - xThis) ** 2.0) + ((yThat - yThis) ** 2.0))
    
let selectClosest candidates origin =
    candidates
    |> Array.zip [| 0 .. candidates.Length - 1 |]
    |> Array.reduce (fun (i, a) (j, b) ->
        if distance a origin <= distance b origin then i, a else j, b)
    |> fst
    
let floorDiv a b = a / b |> int

let Euler = 2.71828
    
let getNeighborhood centerIdx radix domain =
    // Impose upper bound on radix to prevent NaN and blocks.
    let radix = match radix with | r when r < 1 -> 1 | _ -> radix
    // Compute circular network distance to center.
    let deltas = [| 0 .. domain - 1 |] |> Array.map (fun v -> abs(centerIdx - v))
    let distances =
        deltas
        |> Array.map (fun v -> domain - v)
        |> Array.zip deltas
        |> Array.map (fun (a, b) -> min a b)
    // Computer Gaussian distribution around given center.
    Array.zip distances distances
    |> Array.map (fun (a, b) ->
        Euler ** ((-1.0 * (float (a * b))) / (2.0 * (float radix * float radix))))   

let updateNetwork gaussian network winnerCoords learningRate =
    network
    |> Array.map (fun (x, y) -> fst winnerCoords - x, snd winnerCoords - y)
    |> Array.map (fun (x, y) -> x * learningRate, y * learningRate)
    |> Array.zip gaussian
    |> Array.map (fun (v, (x, y)) -> v * x, v * y)
    
let writeOut (network: (float * float)[]) filePath =
    let text =
        network
        |> Array.map (fun (x, y) -> sprintf $"{x},{y}")
        |> String.concat "\n"
    IO.File.WriteAllText(filePath, text)

let main filePath =
    let allCoords = parseCoords filePath |> normalizeCoords
    let popSize = allCoords.Length * 8
    let network = generateNetwork popSize
    let iters = 100000
    let finalNetwork, _, _ =
        [ 0 .. iters ]
        |> List.fold (fun (network, learningRate, popSize) iter ->
            if iter % 1000 = 0 then printf $"{truncate ((float iter / float iters) * 100.0)}%%...\n"
            let winnerCoords = getRandomItem allCoords
            let winnerIdx = selectClosest network winnerCoords
            let gaussian = getNeighborhood winnerIdx (floorDiv popSize 10.0) network.Length
            updateNetwork gaussian network winnerCoords learningRate,
            learningRate * 0.99997,
            popSize * 0.9997) (network, 0.8, float popSize)
    writeOut finalNetwork "out/final_network.csv"
    Ok "Success"

let Cli =
    match Environment.GetCommandLineArgs() |> Array.toList with
    | _::_::fn::rest when rest.Length = 0 ->
        match IO.File.Exists fn with
        | false -> Error $"File does not exist: {fn}"
        | true -> main fn 
    | _ -> Error "Incorrect number of arguments"
    
[<EntryPoint>]
Cli |> printfn "%A"
    

    

    
