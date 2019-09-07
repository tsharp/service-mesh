#!/bin/bash

docker build . -t service-mesh-httptunnel:dev
docker run service-mesh-httptunnel:dev
