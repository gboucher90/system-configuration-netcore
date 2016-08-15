#!/usr/bin/env bash

#exit if any command fails
set -e

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

dotnet restore
cd ./test/System.Configuration.Test/
dotnet test -c Release -f netcoreapp1.0
cd ../..
dotnet pack ./src/System.Configuration -c Release -o ./artifacts