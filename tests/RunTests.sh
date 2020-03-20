#!/bin/bash
# dotnet build --no-restore

cd tests

export DBBACKEND=sqlite
echo "Running tests with backend $DBBACKEND"
dotnet test # -v n --no-build

export DBBACKEND=mssql
echo "Running tests with backend $DBBACKEND"
dotnet test # -v n --no-build

export DBBACKEND=mysql
echo "Running tests with backend $DBBACKEND"
dotnet test # -v n --no-build