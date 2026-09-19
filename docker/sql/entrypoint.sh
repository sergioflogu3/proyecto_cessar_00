#!/bin/bash
set -e

# Detect sqlcmd path (older mssql-tools or newer mssql-tools18)
if [ -f /opt/mssql-tools18/bin/sqlcmd ]; then
    SQLCMD=/opt/mssql-tools18/bin/sqlcmd
elif [ -f /opt/mssql-tools/bin/sqlcmd ]; then
    SQLCMD=/opt/mssql-tools/bin/sqlcmd
else
    echo "ERROR: sqlcmd not found. Installing mssql-tools18..."
    apt-get update && apt-get install -y curl gnupg2
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
    curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list | tee /etc/apt/sources.list.d/mssql-release.list
    apt-get update
    ACCEPT_EULA=Y apt-get install -y mssql-tools18
    SQLCMD=/opt/mssql-tools18/bin/sqlcmd
fi

# Start SQL Server in background
/opt/mssql/bin/sqlservr &
MSSQL_PID=$!

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to start..."
for i in {1..90}; do
    if $SQLCMD -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo "SQL Server is ready!"
        break
    fi
    sleep 1
done

# Run initialization script (create database)
echo "Running initialization script..."
$SQLCMD -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d master -i /docker-entrypoint-initdb.d/init.sql

echo "Database SISTickets created successfully."

# Keep the container running
wait $MSSQL_PID
