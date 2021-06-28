pwsh -Command {
ipmo .\out\BicepNet.PS\
$Result = Build-BicepFile -Path 'C:\git\github\BicepPowerShell\appSettings.bicep'
$Result.Template
} -NoExit