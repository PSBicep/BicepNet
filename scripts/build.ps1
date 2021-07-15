param(
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [Switch]
    $Full
)

$netcoreversion = 'net5.0'

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

$Path = "$ProjectRoot/BicepNet.Core"
Push-Location -Path $Path
Write-Host $Path -ForegroundColor 'Magenta'
if($Full) {
    dotnet build-server shutdown
    dotnet clean
}
dotnet publish -c $Configuration
Pop-Location

$Path = "$ProjectRoot/BicepNet.PS"
Push-Location -Path $Path
Write-Host $Path -ForegroundColor 'Magenta'
if($Full) {
    dotnet build-server shutdown
    dotnet clean
}
dotnet publish -c $Configuration -f $netcoreversion
Pop-Location

$commonFiles = [System.Collections.Generic.HashSet[string]]::new()
Copy-Item -Path "$ProjectRoot/BicepNet.PS/BicepNet.PS.psd1" -Destination $outPath

Get-ChildItem -Path "$ProjectRoot/BicepNet.Core/bin/$Configuration/netstandard2.1/publish" |
Where-Object { $_.Extension -in '.dll', '.pdb' } |
ForEach-Object { 
    [void]$commonFiles.Add($_.Name); 
    Copy-Item -LiteralPath $_.FullName -Destination $commonPath 
}

Get-ChildItem -Path "$ProjectRoot/BicepNet.PS/bin/$Configuration/$netcoreversion/publish" |
Where-Object { $_.Extension -in '.dll', '.pdb' -and -not $commonFiles.Contains($_.Name) } |
ForEach-Object { 
    Copy-Item -LiteralPath $_.FullName -Destination $corePath
}

Compress-Archive -Path $outPath -DestinationPath "$ProjectRoot/out/BicepNet.PS.zip" -Force