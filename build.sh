#!/usr/bin/env bash

#exit if any command fails
set -e

artifactsFolder="./artifacts"

if [ -d $artifactsFolder ]; then
  rm -R $artifactsFolder
fi

dotnet restore
dotnet build ./src/* -c Release
dotnet test ./test/* -c Release
dotnet pack ./src/* -c Release -o ./artifacts