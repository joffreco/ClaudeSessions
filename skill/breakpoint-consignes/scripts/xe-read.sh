#!/usr/bin/env bash
# usage: xe-read.sh <session_name> <repo_dir> [minutes=5] [pattern] [fail]
# Print the statements captured in the last <minutes>: Eastern time | duration ms | rows | result | text.
# <pattern> is a grep -E alternation (a|b, never a\|b); empty matches everything.
# With "fail" as fifth argument, only statements whose result is not OK are listed.
set -euo pipefail
name=${1:?session_name}; repo=${2:?repo_dir}; min=${3:-5}; pat=${4:-}; mode=${5:-}
here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
failfilter=""
[ "$mode" = fail ] && failfilter="AND d.value('(event/data[@name=\"result\"]/text)[1]','varchar(20)') <> 'OK'"
"$here/sqlcmd.sh" "$repo" "
SET NOCOUNT ON;
WITH x AS (SELECT CAST(event_data AS XML) d FROM sys.fn_xe_file_target_read_file(N'/var/opt/mssql/log/$name*.xel', NULL, NULL, NULL))
SELECT CONVERT(varchar(23), CAST(d.value('(event/@timestamp)[1]','datetime2') AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time' AS datetime2), 121)
  + ' | ' + CAST(d.value('(event/data[@name=\"duration\"]/value)[1]','bigint')/1000 AS varchar(20)) + 'ms | rows '
  + ISNULL(CAST(d.value('(event/data[@name=\"row_count\"]/value)[1]','bigint') AS varchar(20)),'?')
  + ' | ' + ISNULL(d.value('(event/data[@name=\"result\"]/text)[1]','varchar(20)'),'?')
  + ' | ' + REPLACE(REPLACE(COALESCE(d.value('(event/data[@name=\"batch_text\"]/value)[1]','nvarchar(max)'), d.value('(event/data[@name=\"statement\"]/value)[1]','nvarchar(max)')), CHAR(10),' '), CHAR(13),' ')
FROM x WHERE d.value('(event/@timestamp)[1]','datetime2') > DATEADD(minute, -$min, SYSUTCDATETIME()) $failfilter
ORDER BY d.value('(event/@timestamp)[1]','datetime2');" | grep -E -- "${pat:-.}" || true
