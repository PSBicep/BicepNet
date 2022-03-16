@{

    # Script module or binary module file associated with this manifest.
    RootModule           = 'Module.NetCore/BicepNet.PS.dll'

    # Version number of this module.
    ModuleVersion        = '2.0.2'

    # Supported PSEditions
    CompatiblePSEditions = @('Core')

    # ID used to uniquely identify this module
    GUID                 = 'e36e6970-03f9-46ba-961f-86aae67933f8'

    # Author of this module
    Author               = 'Simon Wåhlin'

    # Company or vendor of this module
    CompanyName          = 'simonw.se'

    # Copyright statement for this module
    Copyright            = '(c) 2021 Simon Wåhlin. All rights reserved.'

    # Description of the functionality provided by this module
    # Description = 'A thin wrapper around bicep.'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion    = '7.2'

    # Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
    # DotNetFrameworkVersion = '4.6.1'

    # Assemblies that must be loaded prior to importing this module
    RequiredAssemblies = @()

    # Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
    FunctionsToExport    = @()

    # Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
    CmdletsToExport      = @(
        'Get-BicepNetVersion'
        'Get-BicepNetCachePath'
        'Build-BicepNetFile'
        'ConvertTo-BicepNetFile'
        'Publish-BicepNetFile'
        'Restore-BicepNetFile'
        'Find-BicepNetModule'
    )

    # Variables to export from this module
    VariablesToExport    = @()

    # Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
    AliasesToExport      = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData          = @{
        PSData = @{
            Prerelease = ''
        } # End of PSData hashtable

    } # End of PrivateData hashtable

}

