# BicepNet

This is the repository for **BicepNet**, a thin wrapper around [Bicep](https://github.com/Azure/bicep) that will load all Bicep assemblies in a separate context to avoid conflicts with other modules. **BicepNet** is developed for the [Bicep PowerShell](https://github.com/PSBicep/BicepPowerShell) module but could be used for any other project where you want to leverage Bicep functionality natively in PowerShell or .NET.

Using BicepNet is generally much faster than calling the CLI since the overhead of loading all assemblies is only performed once. Since BicepNet depends on internal code from the Bicep project, support for new versions of Bicep is incorporated with a bit of delay. The table below shows wich version of Bicep is used in each release of BicepNet.

## Bicep assembly versions

| BicepNet version | Bicep assembly version |
| --- | --- |
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

## Issues

Issues is disabled in the BicepNet repository. Open an issue in [PSBicep/PSBicep](https://github.com/PSBicep/PSBicep/issues) for bug reports or feature requests.

## Contributing
If you like the BicepNet PowerShell module and want to contribute you are very much welcome to do so. Please read our [Contribution Guide](CONTRIBUTING.md) before you start! ❤

## Maintainers

This project is currently maintained by the following coders:

- [SimonWahlin](https://github.com/SimonWahlin)
- [PalmEmanuel](https://github.com/PalmEmanuel)
- [StefanIvemo](https://github.com/StefanIvemo)
