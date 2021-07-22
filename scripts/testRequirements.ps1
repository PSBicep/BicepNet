$requirements = @(
    @{
        Name = 'Pester'
        RequiredVersion = '5.2.2'
    }
)

$ModulesPath = "$PSScriptRoot\..\TMP\Modules"
if(-not (Test-Path -Path $ModulesPath)) {
    $null = New-Item -ItemType Directory -Force -Path $ModulesPath
}
foreach($req in $requirements) {
    Save-Module @req -Path $ModulesPath -Force
}


