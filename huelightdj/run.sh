#!/bin/sh
set -e

# Home Assistant automatically mounts a persistent volume at /data for every
# app. Point the SQLite database at it so bridge/light data survives restarts.
mkdir -p /data

export ConnectionStrings__DefaultConnection="Data Source=/data/HueEntertainmentPro.db;Cache=Shared"
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_URLS="http://+:8080"

exec dotnet HueEntertainmentPro.Server.dll
