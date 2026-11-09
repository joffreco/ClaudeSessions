#!/usr/bin/env bash
# usage: xe-stop.sh <session_name> <repo_dir>
# Drop the Extended Events session and delete its .xel files; prints 0 when the session is gone.
set -euo pipefail
name=${1:?session_name}; repo=${2:?repo_dir}
here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
"$here/sqlcmd.sh" "$repo" "IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = N'$name') DROP EVENT SESSION $name ON SERVER;
SET NOCOUNT ON; SELECT COUNT(*) FROM sys.server_event_sessions WHERE name = N'$name';"
docker exec -u 0 "${SQL_CONTAINER:-comunik-sql-db}" bash -c "rm -f /var/opt/mssql/log/$name*.xel"
