task GenerateTestCases {
    Import-Module (Convert-Path 'output/BicepNet.PS/2.1.0/BicepNet.PS.psd1')
    $ModuleCommands = Get-Command -Module 'BicepNet.PS'
    # $CommandTable = @{}
    $CommandList = foreach ($Command in $ModuleCommands) {
        # $CommandTable[$Command.Name] = @{}
        # $Command.parameters.Keys | ForEach-Object {$CommandTable[$Command.Name][$_] = $Command.parameters[$_].ParameterType.FullName}
        [PSCustomObject]@{
            CommandName = $Command.Name
            Parameters = $Command.parameters.Keys | ForEach-Object {
                [PSCustomObject]@{
                    ParameterName = $_
                    ParameterType = $Command.parameters[$_].ParameterType.FullName
                }
            }
        }
    }
    $CommandList | ConvertTo-Json -Depth 3 | Out-File 'BicepNet.PS/tests/BicepNet.PS.ParameterTests.json'
}
