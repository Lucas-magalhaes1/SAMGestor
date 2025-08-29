# BASE (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# BUILD
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia apenas os csproj para cache de restore
COPY ["services/payment/src/SAMGestor.Payment.API/SAMGestor.Payment.API.csproj", "services/payment/src/SAMGestor.Payment.API/"]
COPY ["services/payment/src/SAMGestor.Payment.Application/SAMGestor.Payment.Application.csproj", "services/payment/src/SAMGestor.Payment.Application/"]
COPY ["services/payment/src/SAMGestor.Payment.Domain/SAMGestor.Payment.Domain.csproj", "services/payment/src/SAMGestor.Payment.Domain/"]
COPY ["services/payment/src/SAMGestor.Payment.Infrastructure/SAMGestor.Payment.Infrastructure.csproj", "services/payment/src/SAMGestor.Payment.Infrastructure/"]
COPY ["shared/contracts/SAMGestor.Contracts/SAMGestor.Contracts.csproj", "shared/contracts/SAMGestor.Contracts/"]

RUN dotnet restore "services/payment/src/SAMGestor.Payment.API/SAMGestor.Payment.API.csproj"

COPY . .
WORKDIR "/src/services/payment/src/SAMGestor.Payment.API"
RUN dotnet build "SAMGestor.Payment.API.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SAMGestor.Payment.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SAMGestor.Payment.API.dll"]
