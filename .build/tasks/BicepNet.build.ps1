param (
    [string[]]
    $Path = @('BicepNet.Core', 'BicepNet.PS'),

    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Release',

    [Switch]
    $Full,

    [switch]
    $ClearNugetCache
)

task dotnetBuild {
    $CommonFiles = [System.Collections.Generic.HashSet[string]]::new()
    if($ClearNugetCache) {
        dotnet nuget locals all --clear
    }
    if ($Full) {
        dotnet build-server shutdown
    }

    foreach ($projPath in $Path) {
        $outPathFolder = Split-Path -Path (Resolve-Path -Path $projPath) -Leaf
        Write-Host $projPath
        Write-Host $outPathFolder
        $outPath = "bin/$outPathFolder"
        if (-not (Test-Path -Path $projPath)) {
            throw "Path '$projPath' does not exist."
        }

        Push-Location -Path $projPath

        # Remove output folder if exists
        if (Test-Path -Path $outPath) {
            Remove-Item -Path $outPath -Recurse -Force
        }

        Write-Host "Restoring '$projPath'" -ForegroundColor 'Magenta'
        dotnet restore --force-evaluate
        Write-Host "Building '$projPath' to '$outPath'" -ForegroundColor 'Magenta'
        dotnet publish -c $Configuration -o $outPath

        # Remove everything we don't need from the build
        Get-ChildItem -Path $outPath |
            Foreach-Object {
                if ($_.Extension -notin '.dll', '.pdb' -or $CommonFiles.Contains($_.Name)) {
                    # Only keep DLLs and PDBs, and only keep one copy of each file.
                    Remove-Item $_.FullName -Recurse -Force
                }
                else {
                    [void]$CommonFiles.Add($_.Name)
                }
            }

        Pop-Location
    }

    # Hack to get the logging abstractions DLL into the PS module instead of the ALC
    Move-Item "BicepNet.Core/bin/BicepNet.Core/Microsoft.Extensions.Logging.Abstractions.dll" "BicepNet.PS/bin/BicepNet.PS" -ErrorAction 'Ignore'
}
