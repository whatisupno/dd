#!/usr/bin/env bash
# Downloads an AdventureWorks backup (if needed), starts SQL Server in Docker,
# and restores the backup. Safe to re-run: skips the restore if the DB exists.
# Use FORCE=1 to overwrite an existing database.
set -euo pipefail

cd "$(dirname "$0")/.."

if [ -f .env ]; then
  set -a; source .env; set +a
fi
: "${MSSQL_SA_PASSWORD:?Copy .env.example to .env and set MSSQL_SA_PASSWORD}"
DB_NAME="${DB_NAME:-AdventureWorks2025}"
BAK_URL="https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/${DB_NAME}.bak"

mkdir -p backups
if [ ! -f "backups/${DB_NAME}.bak" ]; then
  echo "Downloading ${DB_NAME}.bak ..."
  curl -L --fail -o "backups/${DB_NAME}.bak" "$BAK_URL"
fi

echo "Starting SQL Server container ..."
docker compose up -d

# Find sqlcmd inside the container (the path differs between image versions)
SQLCMD=""
for p in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
  if docker exec dev-sql test -x "$p"; then SQLCMD="$p"; break; fi
done
if [ -z "$SQLCMD" ]; then
  cat <<EOF
sqlcmd isn't available inside this SQL Server image.
Restore manually instead: connect with SSMS or Azure Data Studio to
localhost,1433 (user sa) and restore /backups/${DB_NAME}.bak
(the ./backups folder is mounted into the container at /backups).
EOF
  exit 1
fi

run_sql() {
  docker exec -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" dev-sql \
    "$SQLCMD" -S localhost -U sa -C -b "$@"
}

echo "Waiting for SQL Server to accept connections ..."
for i in $(seq 1 60); do
  if run_sql -Q "SELECT 1" >/dev/null 2>&1; then break; fi
  sleep 2
  if [ "$i" -eq 60 ]; then echo "SQL Server did not start in time"; exit 1; fi
done

EXISTS=$(run_sql -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'${DB_NAME}'" | tr -d '[:space:]')
if [ "$EXISTS" = "1" ] && [ "${FORCE:-0}" != "1" ]; then
  echo "${DB_NAME} already exists. Re-run with FORCE=1 to overwrite it."
  exit 0
fi

echo "Reading logical file names from the backup ..."
FILES=$(run_sql -h -1 -W -s "|" -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'/backups/${DB_NAME}.bak'")

# Build a MOVE clause for each file: D = data, L = log, anything else = folder
MOVES=$(printf '%s\n' "$FILES" | tr -d '\r' | awk -F'|' '
  NF > 3 {
    ext = ($3 == "L") ? ".ldf" : ($3 == "D") ? ".mdf" : ""
    printf "MOVE N\047%s\047 TO N\047/var/opt/mssql/data/%s%s\047, ", $1, $1, ext
  }')

echo "Restoring ${DB_NAME} ..."
run_sql -Q "RESTORE DATABASE [${DB_NAME}] FROM DISK = N'/backups/${DB_NAME}.bak' WITH ${MOVES} REPLACE, STATS = 10"

echo
echo "Done. Connect to localhost,1433 as sa, database ${DB_NAME}."
