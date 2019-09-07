#!/bin/bash

dotnet build
dotnet ./service-mesh-httplistener/bin/Debug/netcoreapp2.1/service-mesh-httplistener.dll
