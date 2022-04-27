#r "nuget: Plotly.NET,  2.0.0-preview.8"
#r "nuget: Plotly.NET.ImageExport, 2.0.0-preview.7"

open System
open Plotly.NET
open Plotly.NET.ImageExport

/// Parse coordinates from csv file having coordinates in second and third
/// columns. Input file should also contain a header, otherwise first node is
/// ignored.
let parseCoords (fn: string) =
    [| use reader = new IO.StreamReader(fn)
       while not reader.EndOfStream do yield reader.ReadLine() |]
    |> (fun arr -> arr.[1 .. arr.Length])
    |> Array.toList
    |> List.map (fun line -> line.Split "," |> Array.toList)
    |> List.map (fun line ->
        match line with
        | _::y::x::_ -> float x, float y
        | _ -> Exception "Incorrect format input file" |> raise)
    |> List.toArray

/// For a given array of 2-dimensions, normalize the two dimensions by
/// removing the initial offset and normalizing the points in a proportional
/// inverval [0, 1] on y, maintaining the original ratio on x.
let normalizeCoords coords =
    let x = coords |> Array.map (fun (x, _) -> x)
    let y = coords |> Array.map (fun (_, y) -> y)
    let xMax, xMin = Array.reduce max x, Array.reduce min x
    let yMax, yMin = Array.reduce max y, Array.reduce min y
    let ratio = (xMax - xMin) / (yMax - yMin)
    let maxRatio = max ratio 1.0
    let xRatio, yRatio = ratio / maxRatio, 1.0 / maxRatio
    
    // Min-max normalize on columns.
    let xNorm = x |> Array.map (fun v -> (v - xMin) / (xMax - xMin))
    let yNorm = y |> Array.map (fun v -> (v - yMin) / (yMax - yMin))
    
    // Apply ratio on rows.
    Array.zip xNorm yNorm
    |> Array.map (fun (x, y) -> xRatio * x, yRatio * y)

let getRandomItem (arr: array<'T>) =
    let rnd = Random()
    Array.item (rnd.Next(arr.Length)) arr

/// Initialise self-organising map.
let generateNetwork size =
    let uniformDistr = [| 0.0 .. 1.0 / 10000.0 .. 1.0 |]
    let xCoords = [| for _ in [ 1 .. size ] do getRandomItem uniformDistr |]
    let yCoords = [| for _ in [ 1 .. size ] do getRandomItem uniformDistr |] 
    Array.zip xCoords yCoords

/// Calculate Euclidean distance between points.
let distance (xThis, yThis) (xThat, yThat) =
    sqrt(((xThat - xThis) ** 2.0) + ((yThat - yThis) ** 2.0))
    
let selectClosest candidates origin =
    candidates
    |> Array.zip [| 0 .. candidates.Length - 1 |]
    |> Array.reduce (fun (i, a) (j, b) ->
        if distance a origin <= distance b origin then i, a else j, b)
    |> fst

/// Floor division -- uses behavior of casting float to int which truncates val.
let floorDiv a b = a / b |> int

/// Approximation Euler's number.
let Euler = 2.71828
    
let getNeighborhood centerIdx radix domain =
    // Impose upper bound on radix to prevent NaN and blocks.
    let radix = match radix with | r when r < 1 -> 1 | _ -> radix
    
    // Compute circular network distance to center.
    let deltas =
        [| 0 .. domain - 1 |]
        |> Array.map (fun v -> abs(centerIdx - v))  
        
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
    // Apply update to network.
    |> Array.zip network
    |> Array.map (fun ((xOld, yOld), (xNew, yNew)) ->
        xOld + xNew, yOld + yNew)
    
let visualizeNetwork iter (network: (float * float)[]) =
    let x = network |> Array.map (fun (x, _) -> x)
    let y = network |> Array.map (fun (_, y) -> y)
    Chart.Scatter(x, y, mode=StyleParam.Mode.Lines_Markers)
    |> Chart.withXAxisStyle ("Normalized latitude")
    |> Chart.withYAxisStyle ("Normalized longitude")
    // |> Chart.withSize(800.0, 500.0)
    // |> Chart.saveHtmlAs $"out/output_iter_{iter}"
    |> Chart.savePNG(
        $"out/output_iter_{iter}",
        Width=800,
        Height=500
    )

let writeOut (network: (float * float)[]) filePath =
    let text =
        network
        |> Array.map (fun (x, y) -> sprintf $"{x},{y}")
        |> String.concat "\n"
    IO.File.WriteAllText(filePath, text)

let main filePath =
    // Parse coordinates from input file.
    let allCoords = parseCoords filePath |> normalizeCoords

    // Initialize self-organising map.
    let popSize = allCoords.Length * 8
    let network = generateNetwork popSize

    // Hyperparameters.
    let iters = 100000
    let learningRate = 0.8
    let learningRateDecay = 0.99997
    let popSizeDecay = 0.9997

    // Iteratively update self-organising map.
    let finalNetwork, _, _ =
        [ 0 .. iters ]
        |> List.fold (fun (network, learningRate, popSize) iter ->
            if iter % 1000 = 0 then 
                [ $"\n{truncate ((float iter / float iters) * 100.0)}%%...";
                  $"\tlearning_rate: {learningRate}";
                  $"\tpopulation_size: {popSize}\n" ]
                |> String.concat "\n"
                |> printf "%s"

            let newLearningRate = learningRate * learningRateDecay
            let newPopSize = popSize * popSizeDecay

            match newPopSize, newLearningRate with
            // Finish optimization when parameter(s) have decayed.
            | n, lr when n < 1.0 || lr < 0.001 -> network, learningRate, popSize
            | _ ->
                let winnerCoords = getRandomItem allCoords
                let winnerIdx = selectClosest network winnerCoords
                let gaussian = getNeighborhood winnerIdx (floorDiv popSize 10.0) network.Length
                let newNetwork = updateNetwork gaussian network winnerCoords learningRate

                // Visualize new map.
                if iter % 5000 = 0 then visualizeNetwork iter newNetwork 

                newNetwork, newLearningRate, newPopSize
        ) (network, learningRate, float popSize)


    // Visualize and write out final map.
    visualizeNetwork iters finalNetwork
    writeOut finalNetwork "out/output.csv"

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
