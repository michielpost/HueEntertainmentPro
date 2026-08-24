# Build stage: compile the .NET server.
# The build context for a Home Assistant add-on is the repository root, so the
# COPY paths below are relative to the repository root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
RUN dotnet publish "./HueEntertainmentPro.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage.
# Home Assistant injects the correct base image through the BUILD_FROM build
# argument (configured per-arch in config.yaml). A sensible default is kept so
# that manual docker builds work out of the box.
ARG BUILD_FROM=mcr.microsoft.com/dotnet/aspnet:10.0
FROM $BUILD_FROM

WORKDIR /app
COPY --from=build /app/publish .
COPY addons/huelightdj/run.sh /run.sh
RUN chmod a+x /run.sh

EXPOSE 8080

CMD ["/run.sh"]
