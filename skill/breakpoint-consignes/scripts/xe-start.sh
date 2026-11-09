#!/usr/bin/env bash
# usage: xe-start.sh <session_name> <database> <repo_dir>
# Create and start an Extended Events session capturing every batch and RPC completed on <database>,
# written to /var/opt/mssql/log/<session_name>*.xel. Fails if the session already exists.
set -euo pipefail
name=${1:?session_name}; db=${2:?database}; repo=${3:?repo_dir}
here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
"$here/sqlcmd.sh" "$repo" "
CREATE EVENT SESSION $name ON SERVER
ADD EVENT sqlserver.sql_batch_completed (ACTION (sqlserver.client_app_name) WHERE sqlserver.database_name = N'$db'),
ADD EVENT sqlserver.rpc_completed (ACTION (sqlserver.client_app_name) WHERE sqlserver.database_name = N'$db')
ADD TARGET package0.event_file (SET filename = N'/var/opt/mssql/log/$name.xel', max_file_size = 50, max_rollover_files = 2)
WITH (STARTUP_STATE = OFF, MAX_DISPATCH_LATENCY = 1 SECONDS);
ALTER EVENT SESSION $name ON SERVER STATE = START;
SELECT name FROM sys.dm_xe_sessions WHERE name = N'$name';"
