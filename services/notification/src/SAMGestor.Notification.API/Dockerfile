FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["services/notification/src/SAMGestor.Notification.API/SAMGestor.Notification.API.csproj", "services/notification/src/SAMGestor.Notification.API/"]
COPY ["services/notification/src/SAMGestor.Notification.Application/SAMGestor.Notification.Application.csproj", "services/notification/src/SAMGestor.Notification.Application/"]
COPY ["services/notification/src/SAMGestor.Notification.Domain/SAMGestor.Notification.Domain.csproj", "services/notification/src/SAMGestor.Notification.Domain/"]
COPY ["services/notification/src/SAMGestor.Notification.Infrastructure/SAMGestor.Notification.Infrastructure.csproj", "services/notification/src/SAMGestor.Notification.Infrastructure/"]
COPY ["shared/contracts/SAMGestor.Contracts/SAMGestor.Contracts.csproj", "shared/contracts/SAMGestor.Contracts/"]

RUN dotnet restore "services/notification/src/SAMGestor.Notification.API/SAMGestor.Notification.API.csproj"

COPY . .
WORKDIR "/src/services/notification/src/SAMGestor.Notification.API"
RUN dotnet build "./SAMGestor.Notification.API.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SAMGestor.Notification.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SAMGestor.Notification.API.dll"]
