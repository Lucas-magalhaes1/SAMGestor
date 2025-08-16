FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SAMGestor.API/SAMGestor.API.csproj", "src/SAMGestor.API/"]
COPY ["src/SAMGestor.Application/SAMGestor.Application.csproj", "src/SAMGestor.Application/"]
COPY ["src/SAMGestor.Domain/SAMGestor.Domain.csproj", "src/SAMGestor.Domain/"]
COPY ["src/SAMGestor.Infrastructure/SAMGestor.Infrastructure.csproj", "src/SAMGestor.Infrastructure/"]
COPY ["shared/contracts/SAMGestor.Contracts/SAMGestor.Contracts.csproj","shared/contracts/SAMGestor.Contracts/"]
COPY ["shared/building-blocks/SAMGestor.BuildingBlocks/SAMGestor.BuildingBlocks.csproj","shared/building-blocks/SAMGestor.BuildingBlocks/"]

RUN dotnet restore "src/SAMGestor.API/SAMGestor.API.csproj"


COPY . .
WORKDIR "/src/src/SAMGestor.API"
RUN dotnet build "./SAMGestor.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SAMGestor.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SAMGestor.API.dll"]
