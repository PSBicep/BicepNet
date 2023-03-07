param(
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Debug',

    [string]
    $Version,

    [Switch]
    $Full,

    [switch]
    $ClearNugetCache,

    [switch]
    $PublishArtifact
)

$netcoreversion = 'net6.0'

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
if($ClearNugetCache) {
    dotnet nuget locals all --clear
}
if ($Full) {
    dotnet build-server shutdown
    dotnet clean
}
dotnet publish -c $Configuration
Pop-Location

$Path = "$ProjectRoot/BicepNet.PS"
Push-Location -Path $Path
Write-Host $Path -ForegroundColor 'Magenta'
if ($Full) {
    dotnet build-server shutdown
    dotnet clean
}
dotnet publish -c $Configuration -f $netcoreversion
Pop-Location

$commonFiles = [System.Collections.Generic.HashSet[string]]::new()

Get-ChildItem -Path "$ProjectRoot/BicepNet.Core/bin/$Configuration/$netcoreversion/publish" |
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

Copy-Item -Path "$ProjectRoot/BicepNet.PS/Manifest/BicepNet.PS.psd1" -Destination $outPath
if (-not $PSBoundParameters.ContainsKey('Version')) {
    try {
        $Version = gitversion /showvariable LegacySemVerPadded
    }
    catch {
        $Version = [string]::Empty
    }
}

if($Version) {
    $SemVer, $PreReleaseTag = $Version.Split('-')
    Update-ModuleManifest -Path "$outPath/BicepNet.PS.psd1" -ModuleVersion $SemVer -Prerelease $PreReleaseTag
}

Move-Item "$outPath/Bicep/Microsoft.Extensions.Logging.Abstractions.dll" "$outPath/Module.NetCore/" -ErrorAction 'Ignore'

if($PublishArtifact) {
    Compress-Archive -Path $outPath -DestinationPath "$ProjectRoot/BicepNet.PS.zip" -Force
}