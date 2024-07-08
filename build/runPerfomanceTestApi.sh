#!/bin/bash

# Navigate to the PerformanceTests directory
cd ./repositories/DemoProjects/Bachelor/CqrsPerformanceAnalysis/tests/Common/PerformanceTests/

# Run the performance test project
dotnet ./bin/Release/net8.0/publish/PerformanceTests.dll --urls="http://localhost:5017"

# Send a request to the running application
curl 'http://localhost:5017'
curl 'http://localhost:5017/K6Tests/allOfBothApis?checkElastic=false&withWarmUp=true&saveMinimalResults=true'
