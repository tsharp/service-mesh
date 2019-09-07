#!/bin/bash

dotnet build
dotnet ./service-mesh-client/bin/Debug/netcoreapp2.1/service-mesh-client.dll
