#!/bin/bash
set -e

cd /src

dotnet restore
dotnet run --urls http://0.0.0.0:8080
