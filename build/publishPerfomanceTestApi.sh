#!/bin/bash

# Navigate to the project directory
cd ./repositories/DemoProjects/Bachelor/CqrsPerformanceAnalysis

# Remove the specified files
rm ./src/Traditional/Traditional.Api/appsettings.Development.json
rm ./src/Traditional/Traditional.Api/appsettings.json

# Navigate to the PerformanceTests directory
cd ./tests/Common/PerformanceTests/

# Restore, build, and publish the project
dotnet restore PerformanceTests.csproj
dotnet build PerformanceTests.csproj -c Release
dotnet publish PerformanceTests.csproj -c Release /p:UseAppHost=false

# Navigate back to the project root directory
cd ../../..

# Checkout the removed files to restore them
git checkout -- ./src/Traditional/Traditional.Api/appsettings.json
git checkout -- ./src/Traditional/Traditional.Api/appsettings.Development.json
