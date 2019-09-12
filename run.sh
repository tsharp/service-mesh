#!/bin/bash

docker build . -t service-mesh-httptunnel:dev
docker run service-mesh-httptunnel:dev

# dotnet build
# dotnet ./service-mesh-httptunnel/bin/Debug/netcoreapp2.1/service-mesh-httptunnel.dll