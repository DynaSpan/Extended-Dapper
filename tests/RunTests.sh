#!/bin/bash
# dotnet build --no-restore

export DBBACKEND=sqlite
echo "Running tests with backend $DBBACKEND"
dotnet test # -v n --no-build

echo "+=====================================================+"
echo " MSSQL testing is currently disabled due to a bug in   "
echo " the transaction handler on Linux/Docker in SQL Server "
echo " and other time out issues, see issues below:          "
echo "                                                       "
echo " https://github.com/microsoft/mssql-docker/issues/492  "
echo " https://github.com/dotnet/SqlClient/issues/126        "
echo "+=====================================================+"

# export DBBACKEND=mssql
# echo "Running tests with backend $DBBACKEND"
# dotnet test # -v n --no-build

export DBBACKEND=mysql
echo "Running tests with backend $DBBACKEND"
dotnet test # -v n --no-build