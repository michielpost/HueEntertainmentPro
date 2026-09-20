# Home Assistant app image for Hue Entertainment Pro.
#
# IMPORTANT: the Docker build context must be the *repository root*:
#   docker build -f huelightdj/Dockerfile .
#
# Home Assistant never builds this Dockerfile itself: the Supervisor uses the
# app folder as build context (which cannot see the app source), so
# config.yaml references the pre-built multi-arch image published by
# .github/workflows/addon.yml instead ("image:" key).

# Build stage. Pinned to the build platform: "dotnet publish" below produces
# portable, architecture-neutral IL (UseAppHost=false, no RID), so the SDK
# never needs to run under QEMU emulation. Only the final base image is
# architecture-specific.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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

# Runtime stage. Not pinned to a platform: Docker Buildx automatically picks
# the variant matching the target platform (linux/amd64 or linux/arm64).
FROM mcr.microsoft.com/dotnet/aspnet:10.0

LABEL org.opencontainers.image.title="Hue Entertainment Pro" \
      org.opencontainers.image.description="Merge multiple Philips Hue entertainment areas and apply realtime light effects" \
      org.opencontainers.image.source="https://github.com/michielpost/HueEntertainmentPro" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
COPY --from=build /app/publish .
COPY huelightdj/run.sh /run.sh
RUN chmod a+x /run.sh

EXPOSE 8080

CMD ["/run.sh"]
