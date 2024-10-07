# BicepNet

> **BicepNet is no longer a standalone project from PSBicep! The code lives on as its own part of the [Bicep PowerShell module](https://github.com/PSBicep/PSBicep), a hybrid module written in both C# (the part that was previously this project) and PowerShell.**

---

This is the repository for **BicepNet**, a thin wrapper around [Bicep](https://github.com/Azure/bicep) that will load all Bicep assemblies in a separate context to avoid conflicts with other modules. **BicepNet** is developed for the [Bicep PowerShell](https://github.com/PSBicep/PSBicep) module but could be used for any other project where you want to leverage Bicep functionality natively in PowerShell or .NET.

Using BicepNet is generally much faster than calling the CLI since the overhead of loading all assemblies is only performed once. Since BicepNet depends on internal code from the Bicep project, support for new versions of Bicep is incorporated with a bit of delay. The table below shows wich version of Bicep is used in each release of BicepNet.

## Bicep assembly versions

| BicepNet version | Bicep assembly version |
| --- | --- |
| `2.3.1` | `0.24.24` |
| `2.3.0` | `0.22.6` |
| `2.2.0` | `0.21.1` |
| `2.1.0` | `0.18.4` |
| `2.0.10` | `0.11.1` |
| `2.0.9` | `0.10.61` |
| `2.0.8` | `0.9.1` |
| `2.0.7` | `0.8.9` |
| `2.0.6` | `0.7.4` |
| `2.0.5` | `0.6.18` |
| `2.0.4` | `0.6.18` |
| `2.0.3` | `0.5.6` |
| `2.0.2` | `0.4.1318` |
| `2.0.1` | `0.4.1272` |
| `2.0.0` | `0.4.1124` |
| `1.0.7` | `0.4.1008` |
| `1.0.6` | `0.4.1008` |
| `1.0.5` | `0.4.613` |
| `1.0.4` | `0.4.451` |
| `1.0.3` | `0.4.412` |
| `1.0.2` | `0.4.412` |
| `1.0.1` | `0.4.63` |
| `1.0.0` | `0.4.63` |

## Contributing

If you like the Bicep PowerShell module and want to contribute you are very much welcome to do so. Please see the [Bicep PowerShell module](https://github.com/PSBicep/PSBicep) for how to get started.
