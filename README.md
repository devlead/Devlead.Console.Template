# @devlead Console Template

@devlead .NET Console Template is my opinionated template alternative to `dotnet new console`.

Background and more details in my blog post [My preferred .NET console stack](https://www.devlead.se/posts/2021/2021-01-15-my-preferred-console-stack).

## Installation

```PowerShell
dotnet new install Devlead.Console.Template
```

## Usage

Create a new project with same name as folder

```PowerShell
dotnet new devleadconsole
```

Create a new project with specified name
```PowerShell
dotnet new devleadconsole -n MyConsole
```

Create a new project with specified target framework `net8.0` or `net9.0` supported (*default `net9.0`*).
```PowerShell
dotnet new devleadconsole -n MyConsole --framework net8.0
```