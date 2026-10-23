dotnet new sln -n Innovia.IoT
dotnet sln add src/Innovia.Shared/Innovia.Shared.csproj
dotnet sln add src/DeviceRegistry.Api/DeviceRegistry.Api.csproj
dotnet sln add src/Ingest.Gateway/Ingest.Gateway.csproj
dotnet sln add src/Realtime.Hub/Realtime.Hub.csproj
dotnet sln add src/Portal.Adapter/Portal.Adapter.csproj
dotnet sln add src/Rules.Engine/Rules.Engine.csproj
dotnet sln add src/Edge.Simulator/Edge.Simulator.csproj
dotnet sln add tests/DeviceRegistry.Tests/DeviceRegistry.Tests.csproj
