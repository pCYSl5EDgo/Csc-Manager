# csc-manager

This is a command line tool for the purpose of using the Roslyn compiler that correctly handles [IgnoresAccessChecksToAttribute](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/).

## Description

About `IgnoresAccessChecksToAttribute`
[English Article](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/).
[Japanese Article](https://qiita.com/mob-sakai/items/a24780d68a6133be338f).

You can access other assemblies' internal/private members without any restriction.

This tool can
 
 - `enable`s csc.exe to compile codes that contain internal/private access.
 - `disable`s csc.exe not to compile codes that contain internal/private access.
 - `enable-vscode`s csc.exe to compile codes that contain internal/private access.
 - `disable-vscode`s csc.exe not to compile codes that contain internal/private access.

# Install

[Csc-Manager](https://github.com/pCYSl5EDgo/Csc-Manager) is distributed as [dotnet global tool](https://www.nuget.org/packages/Csc-Manager/).

On your terminal, you can install the `csc-manager` by following command.

```
dotnet tool install -g Csc-Manager
```

You can now use `csc-manager` command.

# Required Environment

Above .NET Core 2.1

# Usage

## enable

This command modifies the existing `Microsoft.CodeAnalysis.CSharp.dll` in order to enable `csc.exe` to process `IgnoresAccessChecksToAttribute`.
This saves original data as as a file `Microsoft.CodeAnalysis.CSharp.dll.bytes`.

Following options are all optional.

 - `-directory` default value => `./tools/tools/`
  - Directory contains all assemblies which `Microsoft.CodeAnalysis.CSharp.dll` is dependent on.
 - `-path` default value => empty string
  - `Microsoft.CodeAnalysis.CSharp.dll` path.
  - If it is empty, `Microsoft.CodeAnalysis.CSharp.dll` will be searched in the `-directory` folder.

Sample Code

```
csc-manager enable -directory "C:\Program Files\dotnet\sdk\3.1.101\Roslyn\bincore"
```

## disable

This command restores `Microsoft.CodeAnalysis.CSharp.dll` to its original condition by rename `Microsoft.CodeAnalysis.CSharp.dll.bytes` `Microsoft.CodeAnalysis.CSharp.dll`.

Following options are all optional.

 - `-directory` default value => `./tools/tools/`
  - Directory contains all assemblies which `Microsoft.CodeAnalysis.CSharp.dll` is dependent on.
 - `-path` default value => empty string
  - `Microsoft.CodeAnalysis.CSharp.dll` path.
  - If it is empty, `Microsoft.CodeAnalysis.CSharp.dll` will be searched in the `-directory` folder.

Sample Code

```
csc-manager disable -directory "C:\Program Files\dotnet\sdk\3.1.101\Roslyn\bincore"
```

## enable-vscode

VS Code is a special Editor.
When you use [IgnoresAccessChecksToAttribute](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/), you have a lot of errors related to internal/private access.

No options are provided.

Sample Code

```
csc-manager enable-vscode
```

## disable-vscode

This command restores `Microsoft.CodeAnalysis.CSharp.dll` of OmniSharp to its original condition by rename `Microsoft.CodeAnalysis.CSharp.dll.bytes` `Microsoft.CodeAnalysis.CSharp.dll`.

Sample Code

```
csc-manager disable-vscode
```

# Hint

## Unity

Provide that you use Unity Editor on a windows machine which installed via Unity Hub, you can find the `Microsoft.CodeAnalysis.CSharp.dll` in `C:\Program Files\Unity\Hub\Editor\2020.1.0a14\Editor\Data\Tools\Roslyn`.
Change the version accoring to your environment.

## dotnet core

Provide that you want to enable your `dotnet` command to handle correctly `IgnoresAccessChecksToAttribute`, you can find the `Microsoft.CodeAnalysis.CSharp.dll` in `C:\Program Files\dotnet\sdk\3.1.100\Roslyn\bincore`.
Change the version accoring to your environment.

# LICENSE

MIT

# Special Thanks

Thank you [@mob-sakai](https://github.com/mob-sakai)!
With your [work](https://github.com/mob-sakai/OpenSesameCompilerForUnity), [Japanese articles](https://qiita.com/mob-sakai/items/a24780d68a6133be338f) and discussion you and me, I developed this!