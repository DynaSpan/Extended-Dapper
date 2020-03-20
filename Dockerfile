FROM mcr.microsoft.com/dotnet/core/sdk:3.1
WORKDIR /build

# Add wait to wait for mssql and mysql to come online
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.7.3/wait /wait
RUN /bin/bash -c 'ls -la /wait; chmod +x /wait; ls -la /wait'

# Workaround for bug in SqlClient with timeouts
# https://github.com/dotnet/SqlClient/issues/126
RUN sed -i '/^ssl_conf = ssl_sect$/s/^/#/' /etc/ssl/openssl.cnf

# Restore packages
COPY *.sln .
COPY tests/Extended.Dapper.Tests.csproj ./tests/Extended.Dapper.Tests.csproj
COPY src/Extended.Dapper.Core/Extended.Dapper.Core.csproj ./src/Extended.Dapper.Core/Extended.Dapper.Core.csproj
#RUN dotnet restore

COPY . .
# RUN dotnet build
CMD /wait && cd tests && bash ./RunTests.sh