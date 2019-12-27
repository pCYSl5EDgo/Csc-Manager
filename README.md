# csc-manager

This is a command line tool for the purpose of using the Roslyn compiler that correctly handles [IgnoresAccessChecksToAttribute](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/).

## Description

About `IgnoresAccessChecksToAttribute`
[English Article](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/).
[Japanese Article](https://qiita.com/mob-sakai/items/a24780d68a6133be338f).

You can access other assemblies' internal/private members without any restriction.

This tool can
 
 - `download` csc.exe from [Microsoft's official Nuget package](https://www.nuget.org/packages/Microsoft.Net.Compilers).
 - `enable`s csc.exe to compile codes that contain internal/private access.

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

## download

This command downloads [Microsoft.Net.Compilers](https://www.nuget.org/packages/Microsoft.Net.Compilers) package and extend it to obtain valid compiler.

Following options are all optional

 - `-version` default value => `3.4.0`
  - Csc Version String
  - Latest stable C#8 Compiler is `3.4.0`
 - `-download-file` default value => `./csc.zip`
  - Temporary file name
 - `-directory` default value => `./tools/`
  - Extended zip archive root directory

Example

```
csc-manager download -version 3.4.0
```

## enable

This command modifies the existing `Microsoft.CodeAnalysis.CSharp.dll` in order to enable `csc.exe` to process `IgnoresAccessChecksToAttribute`.

Following options are all optional

 - `-path` default value => empty string
  - `Microsoft.CodeAnalysis.CSharp.dll` path.
  - If it is empty, `Microsoft.CodeAnalysis.CSharp.dll` will be searched in the `-directory` folder.
 - `-directory` default value => `./tools/tools/`
  - Directory contains all assemblies which `Microsoft.CodeAnalysis.CSharp.dll` is dependent on.
 - `-suffix` default value => empty string
  - If it is empty, `Microsoft.CodeAnalysis.CSharp.dll` is modified.
  - If it is not empty, `Microsoft.CodeAnalysis.CSharp.dll` is not modified. Modified dll is generated.


# Hint

## Unity

Provide that you use Unity Editor on a windows machine which installed via Unity Hub, you can find the `Microsoft.CodeAnalysis.CSharp.dll` in `C:\Program Files\Unity\Hub\Editor\2020.1.0a14\Editor\Data\Tools\Roslyn`.
Change the version accoring to your environment.

## dotnet core

Provide that you want to enable your `dotnet` command to handle correctly `IgnoresAccessChecksToAttribute`, you can find the `Microsoft.CodeAnalysis.CSharp.dll` in `C:\Program Files\dotnet\sdk\3.1.100\Roslyn\bincore`.
Change the version accoring to your environment.

## Visual Studio Code - OmniSharp

Provide that you use Visual Studio Code on a windows machine you can find the `Microsoft.CodeAnalysis.CSharp.dll` in `%USERPROFILE%\.vscode\extensions\ms-vscode.csharp-1.21.9\.omnisharp\1.34.9`.
Change the version accoring to your environment.

# LICENSE

MIT

# Special Thanks

Thank you [@mob-sakai](https://github.com/mob-sakai)!
With your [work](https://github.com/mob-sakai/OpenSesameCompilerForUnity), [Japanese articles](https://qiita.com/mob-sakai/items/a24780d68a6133be338f) and discussion you and me, I developed this!