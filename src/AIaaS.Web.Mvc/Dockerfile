#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/AIaaS.Web.Mvc/AIaaS.Web.Mvc.csproj", "src/AIaaS.Web.Mvc/"]
COPY ["src/AIaaS.Web.Core/AIaaS.Web.Core.csproj", "src/AIaaS.Web.Core/"]
COPY ["src/AIaaS.Application/AIaaS.Application.csproj", "src/AIaaS.Application/"]
COPY ["src/AIaaS.Application.Shared/AIaaS.Application.Shared.csproj", "src/AIaaS.Application.Shared/"]
COPY ["src/AIaaS.Core.Shared/AIaaS.Core.Shared.csproj", "src/AIaaS.Core.Shared/"]
COPY ["src/AIaaS.Core/AIaaS.Core.csproj", "src/AIaaS.Core/"]
COPY ["src/AIaaS.EntityFrameworkCore/AIaaS.EntityFrameworkCore.csproj", "src/AIaaS.EntityFrameworkCore/"]
COPY ["src/AIaaS.GraphQL/AIaaS.GraphQL.csproj", "src/AIaaS.GraphQL/"]
RUN dotnet restore "src/AIaaS.Web.Mvc/AIaaS.Web.Mvc.csproj"
COPY . .
WORKDIR "/src/src/AIaaS.Web.Mvc"
RUN dotnet build "AIaaS.Web.Mvc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AIaaS.Web.Mvc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIaaS.Web.Mvc.dll"]
