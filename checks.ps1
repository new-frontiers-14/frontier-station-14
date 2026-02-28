$ErrorActionPreference = "Stop"
Set-PSDebug -Trace 1

dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj --no-build

dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName!~ShipyardTest" -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed

Read-Host -Prompt "Press Enter to continue..."
