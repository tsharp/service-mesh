#!/bin/bash

docker build . -t service-mesh-httptunnel:dev

docker tag service-mesh-httptunnel:dev section9.azurecr.io/service-mesh-httptunnel:latest
docker push section9.azurecr.io/service-mesh-httptunnel:latest

docker run service-mesh-httptunnel:dev

# dotnet build
# dotnet ./service-mesh-httptunnel/bin/Debug/netcoreapp2.1/service-mesh-httptunnel.dll