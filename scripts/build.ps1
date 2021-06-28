param(
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Switch]
    $Full
)

$targetFramework = 'net5.0'

$ProjectRoot = "$PSScriptRoot/.."
$outPath = "$ProjectRoot/out/BicepNet.PS"
$commonPath = "$outPath/Bicep"
$corePath = "$outPath/Module.NetCore"

if (Test-Path $outPath) {
    Remove-Item -Path $outPath -Recurse
}
New-Item -Path $outPath -ItemType Directory
New-Item -Path $commonPath -ItemType Directory
New-Item -Path $corePath -ItemType Directory

if($Full) {
    $Projects = "$ProjectRoot/TMP/bicep/src/Bicep.Core",
                "$ProjectRoot/TMP/bicep/src/Bicep.Decompiler",
                "$ProjectRoot/BicepNet.Core",
                "$ProjectRoot/BicepNet.PS" 
}
else {
    $Projects = "$ProjectRoot/BicepNet.Core",
                "$ProjectRoot/BicepNet.PS" 
}

$Projects | Foreach-Object -Process {
    Write-Host $_ -ForegroundColor 'Magenta'
    if($Full) {
        dotnet build-server shutdown
        dotnet clean
    }
    dotnet publish -c $Configuration $_
}


$commonFiles = [System.Collections.Generic.HashSet[string]]::new()
Copy-Item -Path "$ProjectRoot/BicepNet.PS/BicepNet.PS.psd1" -Destination $outPath

Get-ChildItem -Path "$ProjectRoot/BicepNet.Core/bin/$Configuration/$targetFramework/publish" |
Where-Object { $_.Extension -in '.dll', '.pdb' } |
ForEach-Object { 
    [void]$commonFiles.Add($_.Name); 
    Copy-Item -LiteralPath $_.FullName -Destination $commonPath 
}

Get-ChildItem -Path "$ProjectRoot/BicepNet.PS/bin/$Configuration/$targetFramework/publish" |
Where-Object { $_.Extension -in '.dll', '.pdb' -and -not $commonFiles.Contains($_.Name) } |
ForEach-Object { 
    Copy-Item -LiteralPath $_.FullName -Destination $corePath
}
