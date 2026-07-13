#!/bin/sh
set -eu

RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-14}"
BACKUP_DIR=/backups
IMAGES_DIR=/data/images

# MYSQL_PWD instead of -p on the command line - a CLI password argument is
# visible to anything that can list this container's processes.
export MYSQL_PWD="$MYSQL_ROOT_PASSWORD"

run_backup() {
    ts=$(date +%Y%m%d_%H%M%S)
    echo "[$(date)] Starting backup $ts"

    mysqldump --single-transaction -h db -u root "$MYSQL_DATABASE" \
        | gzip > "$BACKUP_DIR/db_${ts}.sql.gz"

    tar -czf "$BACKUP_DIR/images_${ts}.tar.gz" -C "$IMAGES_DIR" .

    echo "[$(date)] Backup $ts complete: db_${ts}.sql.gz, images_${ts}.tar.gz"

    find "$BACKUP_DIR" -name 'db_*.sql.gz' -mtime "+$RETENTION_DAYS" -delete
    find "$BACKUP_DIR" -name 'images_*.tar.gz' -mtime "+$RETENTION_DAYS" -delete
}

# Run once immediately on startup, then nightly.
run_backup
while true; do
    sleep 86400
    run_backup
done
