#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/AIaaS.Migrator/AIaaS.Migrator.csproj", "src/AIaaS.Migrator/"]
COPY ["src/AIaaS.EntityFrameworkCore/AIaaS.EntityFrameworkCore.csproj", "src/AIaaS.EntityFrameworkCore/"]
COPY ["src/AIaaS.Core/AIaaS.Core.csproj", "src/AIaaS.Core/"]
COPY ["src/AIaaS.Core.Shared/AIaaS.Core.Shared.csproj", "src/AIaaS.Core.Shared/"]
RUN dotnet restore "src/AIaaS.Migrator/AIaaS.Migrator.csproj"
COPY . .
WORKDIR "/src/src/AIaaS.Migrator"
RUN dotnet build "AIaaS.Migrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIaaS.Migrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIaaS.Migrator.dll", "-s"]
