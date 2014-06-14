namespace CompilerThingy

open System
open System.Collections.Generic
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.ErrorLogger

type FSharpCompilerService() = 
    
    let mkCompilationErorHandlers() = 
        let errors = ResizeArray<_>()

        let errorSink warn exn = 
            let mainError,relatedErrors = Build.SplitRelatedErrors exn 
            let oneError trim e = errors.Add(ErrorInfo.CreateFromException (e, warn, trim, Range.range0))
            oneError false mainError
            List.iter (oneError true) relatedErrors

        let errorLogger = 
            { new ErrorLogger("CompileAPI") with 
                member x.WarnSinkImpl(exn) = errorSink true exn
                member x.ErrorSinkImpl(exn) = errorSink false exn
                member x.ErrorCount = errors |> Seq.filter (fun e -> e.Severity = Severity.Error) |> Seq.length }

        let loggerProvider = 
            { new Driver.ErrorLoggerProvider() with 
                member x.CreateErrorLoggerThatQuitsAfterMaxErrors(_tcConfigBuilder, _exiter) = errorLogger    }
        errors, errorLogger, loggerProvider

    let tryCompile errorLogger f = 
        use unwindParsePhase = PushThreadBuildPhaseUntilUnwind (BuildPhase.Parse)            
        use unwindEL_2 = PushErrorLoggerPhaseUntilUnwind (fun _ -> errorLogger)
        let exiter = { new Exiter with member x.Exit n = raise StopProcessing }
        try 
            f exiter
            0
        with e -> 
            stopProcessingRecovery e Range.range0
            1

    member this.Compile(sourceFiles : List<String>, assemblyName: string, assemblyReferences : List<String>) =
        let sourceList = sourceFiles |> Seq.toList
        let dependencyList = assemblyReferences |> Seq.toList
        let outfile = (System.IO.Path.GetTempFileName(), "dll") |> System.IO.Path.ChangeExtension

        let errors, errorLogger, loggerProvider = mkCompilationErorHandlers()
     
        let resultAssembly : byte array option ref = ref None
        let result = 
            tryCompile errorLogger (fun exiter -> 
                resultAssembly := Some(Driver.memoryCompileOfAst (assemblyName, Build.CompilerTarget.Dll, outfile, dependencyList, exiter, loggerProvider, sourceList)))

        match !resultAssembly with
        | Some assemblyData -> assemblyData
        | None -> failwith "It messed up bro."
