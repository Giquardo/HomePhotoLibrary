#!/bin/sh
set -eu

BACKUP_DIR=/backups
IMAGES_DIR=/data/images

export MYSQL_PWD="$MYSQL_ROOT_PASSWORD"

if [ -z "${1:-}" ]; then
    echo "Usage: restore.sh <timestamp>  (e.g. 20260713_120000)"
    echo ""
    echo "Available backups:"
    ls "$BACKUP_DIR" | grep '^db_' | sed 's/^db_//; s/\.sql\.gz$//'
    exit 1
fi

TS="$1"
DB_FILE="$BACKUP_DIR/db_${TS}.sql.gz"
IMAGES_FILE="$BACKUP_DIR/images_${TS}.tar.gz"

if [ ! -f "$DB_FILE" ] || [ ! -f "$IMAGES_FILE" ]; then
    echo "Backup files for timestamp '$TS' not found in $BACKUP_DIR"
    exit 1
fi

echo "Restoring database from $DB_FILE ..."
gunzip -c "$DB_FILE" | mysql -h db -u root "$MYSQL_DATABASE"

echo "Restoring images from $IMAGES_FILE ..."
rm -rf "${IMAGES_DIR:?}"/*
tar -xzf "$IMAGES_FILE" -C "$IMAGES_DIR"

echo "Restore complete."
