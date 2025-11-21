# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HueEntertainmentPro/Server/HueEntertainmentPro.Server.csproj", "HueEntertainmentPro/Server/"]
COPY ["HueEntertainmentPro/Client/HueEntertainmentPro.Client.csproj", "HueEntertainmentPro/Client/"]
COPY ["HueEntertainmentPro/Shared/HueEntertainmentPro.Shared.csproj", "HueEntertainmentPro/Shared/"]
COPY ["HueEntertainmentPro.Services/HueEntertainmentPro.Services.csproj", "HueEntertainmentPro.Services/"]
COPY ["HueEntertainmentPro.Database/HueEntertainmentPro.Database.csproj", "HueEntertainmentPro.Database/"]
COPY ["HueLightDJ.Services.Interfaces/HueLightDJ.Services.Interfaces.csproj", "HueLightDJ.Services.Interfaces/"]
COPY ["HueLightDJ.Services/HueLightDJ.Services.csproj", "HueLightDJ.Services/"]
COPY ["HueLightDJ.Effects/HueLightDJ.Effects.csproj", "HueLightDJ.Effects/"]
RUN dotnet restore "./HueEntertainmentPro/Server/HueEntertainmentPro.Server.csproj"
COPY . .
WORKDIR "/src/HueEntertainmentPro/Server"
RUN dotnet build "./HueEntertainmentPro.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HueEntertainmentPro.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
RUN mkdir -p /App_Data
RUN chmod -R 777 /App_Data # Grant full permissions
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HueEntertainmentPro.Server.dll"]
