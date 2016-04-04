﻿// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @"packages/FAKE/tools/"
#r @"FakeLib.dll"

open Fake
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let solutionFile = "Demonstrator.sln"

let deploymentTemp = getBuildParam "DEPLOYMENT_TEMP"
let deploymentTarget = getBuildParam "DEPLOYMENT_TARGET"
let nextManifestPath = getBuildParam "NEXT_MANIFEST_PATH"
let previousManifestPath = getBuildParam "PREVIOUS_MANIFEST_PATH"
let kuduSyncCmd = getBuildParam "KUDU_SYNC_CMD"
                     
Target "Clean" (fun _ ->
    CreateDir deploymentTemp
    CleanDir deploymentTemp)

Target "BuildSolution" (fun _ ->
    solutionFile
    |> MSBuildHelper.build (fun defaults ->
        { defaults with
            Verbosity = Some Minimal
            Targets = [ "Rebuild" ]
            Properties = [ "Configuration", "Release" ] })
    |> ignore)

Target "CopyWebsite" (fun _ ->
    !! @"src\webhost\**"
    -- @"src\webhost\typings"
    -- @"src\webhost\**\*.fs"
    -- @"src\webhost\**\*.config"
    -- @"src\webhost\**\*.references"
    -- @"src\webhost\tsconfig.json"
    |> FileHelper.CopyFiles deploymentTemp)

Target "DeployWebJob" (fun _ ->
    @"src\Sample.fsx"     
    |> FileHelper.CopyFile (deploymentTemp + @"\app_data\jobs\continuous\Sample\"))

Target "DeployWebsite" (fun _ ->
    ProcessHelper.ExecProcess(fun psi ->
        psi.FileName <- kuduSyncCmd
        psi.Arguments <- sprintf """-v 50 -f "%s" -t "%s" -n "%s" -p "%s" -i ".git;.hg;.deployment;deploy.cmd""" deploymentTemp deploymentTarget nextManifestPath previousManifestPath)
        TimeSpan.Zero
    |> ignore)

"Clean"
==> "CopyWebsite"
==> "BuildSolution"
==> "DeployWebJob"
==> "DeployWebsite"

RunTargetOrDefault "DeployWebsite"