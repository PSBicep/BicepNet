@{

    # Script module or binary module file associated with this manifest.
    RootModule           = 'Module.NetCore/BicepNet.PS.dll'

    # Version number of this module.
    ModuleVersion        = '2.0.8'

    # Supported PSEditions
    CompatiblePSEditions = @('Core')

    # ID used to uniquely identify this module
    GUID                 = 'e36e6970-03f9-46ba-961f-86aae67933f8'

    # Author of this module
    Author               = 'Simon Wåhlin'

    # Company or vendor of this module
    CompanyName          = 'simonw.se'

    # Copyright statement for this module
    Copyright            = '(c) 2023 Simon Wåhlin. All rights reserved.'

    # Description of the functionality provided by this module
    Description = 'A thin wrapper around bicep that will load all Bicep assemblies in a separate context to avoid conflicts with other modules. 
BicepNet is developed for the Bicep PowerShell module but could be used for any other project where you want to leverage Bicep functionality in PowerShell or .NET.'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion    = '7.3'

    # Minimum version of Microsoft .NET Framework required by this module. This prerequisite is valid for the PowerShell Desktop edition only.
    # DotNetFrameworkVersion = '4.6.1'

    # Assemblies that must be loaded prior to importing this module
    RequiredAssemblies = @()

    # Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
    FunctionsToExport    = @()

    # Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
    CmdletsToExport      = @(
        'Build-BicepNetFile'
        'Build-BicepNetParamFile'
        'Clear-BicepNetCredential'
        'Convert-BicepNetResourceToBicep'
        'ConvertTo-BicepNetFile'
        'Export-BicepNetResource'
        'Export-BicepNetChildResource'
        'Find-BicepNetModule'
        'Get-BicepNetAccessToken'
        'Get-BicepNetCachePath'
        'Get-BicepNetConfig'
        'Get-BicepNetVersion'
        'Publish-BicepNetFile'
        'Restore-BicepNetFile'
        'Set-BicepNetCredential'
    )

    # Variables to export from this module
    VariablesToExport    = @()

    # Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
    AliasesToExport      = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData          = @{
        PSData = @{

            # Tags applied to this module. These help with module discovery in online galleries.
            Tags = @('azure', 'bicep', 'arm-json', 'arm-templates', 'windows', 'linux', 'bicepnet', 'psbicep')

            # A URL to the license for this module.
            LicenseUri = 'https://github.com/PSBicep/BicepNet/blob/main/LICENSE'

            #A URL to the main website for this project.
            ProjectUri = 'https://github.com/PSBicep/BicepNet'

            # A URL to an icon representing this module.
            IconUri = 'https://raw.githubusercontent.com/PSBicep/PSBicep/main/logo/BicePS.png'

            # ReleaseNotes of this module
            ReleaseNotes = 'https://github.com/PSBicep/BicepNet/releases'

            # Prerelease string of this module
            # Prerelease = ''

            # Flag to indicate whether the module requires explicit user acceptance for install/update/save
            # RequireLicenseAcceptance = $false

            # External dependent modules of this module
            # ExternalModuleDependencies = @()
        } # End of PSData hashtable

    } # End of PrivateData hashtable

}

