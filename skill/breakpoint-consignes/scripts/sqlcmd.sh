#!/usr/bin/env bash
# usage: sqlcmd.sh <repo_dir> <query>
# Run a query as sa on the dev SQL Server container; the password is the repo .env's first DB_PASS* value.
# The container defaults to comunik-sql-db (override with SQL_CONTAINER).
set -euo pipefail
repo=${1:?repo_dir}; query=${2:?query}
pass=$(grep -E '^DB_PASS[A-Z_]*=' "$repo/.env" | head -1 | cut -d= -f2- | sed -E 's/^"//; s/".*//')
docker exec "${SQL_CONTAINER:-comunik-sql-db}" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$pass" -I -y 0 -Q "$query"
