param(
    $bicepVersion
)

try {
    [version]$DotNetVersion = dotnet --version
    if ($DotNetVersion.Major -lt 6) {
        throw
    }
}
catch {
    throw '.net 6 is required to build BicepNet. Get it at https://dotnet.microsoft.com/en-us/download/dotnet/6.0'
}

if(-not $bicepVersion) {
    $bicepVersion = Get-Content -Path "$PSScriptRoot\..\.bicepVersion"
    $bicepVersion = $bicepVersion.Trim()
}
$LocalTMPPath = "$PSScriptRoot\..\TMP"
if(-not (Test-Path -Path $LocalTMPPath)) {
    $null = New-Item -Path $LocalTMPPath -ItemType 'Directory'
}

Push-Location $LocalTMPPath -StackName 'Dependencies'
if(Test-Path 'bicep') {
    Remove-Item 'bicep' -Recurse -Force
}
git clone 'https://github.com/Azure/bicep.git'
Push-Location 'bicep' -StackName 'Dependencies'
git checkout $bicepVersion

while(Get-Location -Stack -StackName 'Dependencies' -ErrorAction 'Ignore') {
    Pop-Location -StackName 'Dependencies'
}