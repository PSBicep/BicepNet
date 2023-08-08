# Contributing to BicepNet

BicepNet is a thin wrapper around bicep that solves the problem with conflicting assemblies.
The techique is sourced from this blog post: [Resolving PowerShell module assembly dependency conflicts](https://docs.microsoft.com/en-us/powershell/scripting/dev-cross-plat/resolving-dependency-conflicts?view=powershell-7.1)

BicepNet has two parts, BicepNet.Core and BicepNet.PS.

## BicepNet.PS

BicepNet.PS is a PowerShell module written i C# that creates an assembly load context and loads any dependencies of BicepNet.Core into that context.
This will resolve any conflicts with other PowerShell modules that depends on different versions of the same DLL-files as Bicep does.

The goal of BicepNet.PS is also to translate any functionality exposed by BicepNet.Core as PowerShell cmdlets.

Each cmdlet is defined in a separate file in the Commands folder. The more generic load context code is in the LoadContext folder.

## BicepNet.Core

BicepNet.Core will have all it's dependencies loaded into the assembly load context created by BicepNet.PS. That means that PowerShell will not be able to access anything defined directly in bicep or any of it's dependencies. To solve this, BicepNet.Core has to implement and expose any functionallity we want to use from bicep. We have chosen to expose these as static methods in the class BicepNet.Core.BicepWrapper. Each method is implemented in it's own file in the BicepNet.Core folder root.

Any class needed for data returned to BicepNet.PS needs to be defined in the BicepNet.Core as well. These are defined as classes in the Modules folder of BicepNet.Core.

## Setting up a dev-environment

The project ships with a dev container, it should contain everything needed.  
The container build process will clone and build Bicep which takes some time, please be patient.

**Known Issues:**
* There might be an error stating that it failed to restore projects. This is because the language servers tries to restore the project before the dependencies are downloaded. Just ignore the error and restart the container.  

To with with BicepNet locally, follow the instructions below:

### Prerequisites for local setup

1. git ([Download](https://git-scm.com/downloads))
1. dotnet 7 SDK ([Download](https://dotnet.microsoft.com/download))
1. Visual Studio Code ([Download](https://code.visualstudio.com/download))
1. (optional) gitversion ([Download](https://gitversion.net/docs/usage/cli/installation))

### Set up project

1. Make sure your execution policy allows execution of PowerShell scripts.  
`Set-ExecutionPolicy -ExecutionPolicy 'Unrestricted' -Scope 'CurrentUser' -Force`
1. Clone BicepNet repository to your local machine.  
`git clone https://github.com/PSBicep/BicepNet.git`  
1. Run dependencies script to clone and checkout bicep  
`.\scripts\Dependencies.ps1`  
To checkout a specific version of bicep use parameter -bicepVersion and a value that corresponds to a specific tag or commit in the bicep repository.  
For example:  
`.\scripts\Dependencies.ps1 -bicepVersion v0.4.451`  
If no version is specified, the version specified in the file .bicepVersion will be used.  
1. Build the project  

Now you are ready to start coding!

### Build
To build BicepNet, run the script scripts/build.ps1.  
This will create the folder 'output' containing a BicepNet.PS module that is ready to be loaded.  
To use the new version of BicepNet in PSBicep, copy the module to the PSBicep module folder.  
