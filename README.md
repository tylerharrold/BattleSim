# BattleSim

A small .NET 8 Avalonia tactical battle simulator scaffold. The solution keeps combat rules isolated from the desktop UI so the battle engine can evolve and be tested independently.

## Projects

- `BattleSim.Domain`: pure domain models and enums. No UI, Avalonia, or engine references.
- `BattleSim.Engine`: deterministic placeholder battle resolution. Depends only on `BattleSim.Domain`.
- `BattleSim.App`: Avalonia MVVM desktop shell. Depends on `BattleSim.Engine` and `BattleSim.Domain`.
- `BattleSim.Tests`: xUnit tests for engine behavior.

## Equivalent dotnet CLI commands

```bash
dotnet new sln -n BattleSim
dotnet new classlib -n BattleSim.Domain -o src/BattleSim.Domain -f net8.0
dotnet new classlib -n BattleSim.Engine -o src/BattleSim.Engine -f net8.0
dotnet new avalonia.app -n BattleSim.App -o src/BattleSim.App -f net8.0
dotnet new xunit -n BattleSim.Tests -o tests/BattleSim.Tests -f net8.0

dotnet sln BattleSim.sln add src/BattleSim.Domain/BattleSim.Domain.csproj
dotnet sln BattleSim.sln add src/BattleSim.Engine/BattleSim.Engine.csproj
dotnet sln BattleSim.sln add src/BattleSim.App/BattleSim.App.csproj
dotnet sln BattleSim.sln add tests/BattleSim.Tests/BattleSim.Tests.csproj

dotnet add src/BattleSim.Engine/BattleSim.Engine.csproj reference src/BattleSim.Domain/BattleSim.Domain.csproj
dotnet add src/BattleSim.App/BattleSim.App.csproj reference src/BattleSim.Domain/BattleSim.Domain.csproj
dotnet add src/BattleSim.App/BattleSim.App.csproj reference src/BattleSim.Engine/BattleSim.Engine.csproj
dotnet add tests/BattleSim.Tests/BattleSim.Tests.csproj reference src/BattleSim.Domain/BattleSim.Domain.csproj
dotnet add tests/BattleSim.Tests/BattleSim.Tests.csproj reference src/BattleSim.Engine/BattleSim.Engine.csproj
```

## Run

Install the .NET 8 SDK, then run:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/BattleSim.App/BattleSim.App.csproj
```

The app starts with two placeholder 3x3 formations, a battle log, and buttons to run one deterministic turn or reset the battle.
