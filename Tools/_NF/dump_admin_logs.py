#!/usr/bin/env python3

# Admin log dump script 
# Dumps existing logs, optionally compressed, and optionally deletes them

import argparse
import os
import psycopg2
import gzip
import datetime
import calendar

LATEST_DB_MIGRATION = "20240623005121_BanTemplate"

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("out_dir", help="Directory to output data dumps into.")
    parser.add_argument("--date", help="Date to save/remove info until, must be in ISO format, ignores time zones. Defaults to the beginning of the month, 6 calendar months ago.")
    parser.add_argument("--compress", action="store_true", help="If set, compresses the contents of the file in .gzip format.")
    parser.add_argument("--delete", action="store_true", help="If set, deletes the contents of the tables after writing the output.")
    parser.add_argument("--ignore-schema-mismatch", action="store_true")
    parser.add_argument("--connection-string", required=True, help="Database connection string to use. See https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING")

    args = parser.parse_args()

    arg_output: str = args.out_dir

    if not os.path.exists(arg_output):
        print("Creating output directory {arg_output} (doesn't exist yet)")
        os.mkdir(arg_output)

    # Get our old time
    if args.date is None:
        today = datetime.datetime.now()
        if today.month > 6:
            month = today.month - 6
            year = today.year
        else:
            month = today.month + 6
            year = today.year - 1
        end_date: "datetime.datetime" = datetime.datetime(year, month, 1, tzinfo=datetime.timezone.utc)
    else:
        end_date: "datetime.datetime" = datetime.datetime.fromisoformat(args.date)
        if end_date.tzinfo is None:
            end_date = end_date.astimezone(datetime.timezone.utc)

    compressed_string = "compressed" if args.compress else "uncompressed"
    print(f"Exporting {compressed_string} admin logs until {end_date}")

    conn = psycopg2.connect(args.connection_string)
    cur = conn.cursor()

    # Find oldest dated entry - hack: discard time zone info
    oldest_record = get_oldest_admin_log(cur)
    oldest_record = oldest_record.astimezone(None)

    # From this, create your intervals up to the deleted time.
    if oldest_record > end_date:
        print(f"Nothing to export. Oldest record {oldest_record} is older than given date {end_date}")

    old_date = datetime.datetime(oldest_record.year, oldest_record.month, oldest_record.day, tzinfo=datetime.timezone.utc)

    while old_date < end_date:
        new_date = next_month(old_date)
        if new_date > end_date:
            new_date = end_date

        dump_admin_in_range(cur, old_date, new_date, arg_output, args.compress, args.delete)


def next_month(date_in: "datetime.datetime") -> datetime.datetime:
    if (date_in.month == 12):
        return datetime.datetime(date_in.year + 1, 1, date_in.day, tzinfo=datetime.timezone.utc)
    else:
        return datetime.datetime(date_in.year, date_in.month + 1, date_in.day, tzinfo=datetime.timezone.utc)

def check_schema_version(cur: "psycopg2.cursor", ignore_mismatch: bool):
    cur.execute('SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "__EFMigrationsHistory" DESC LIMIT 1')
    schema_version = cur.fetchone()
    if schema_version == None:
        print("Unable to read database schema version.")
        exit(1)

    if schema_version[0] != LATEST_DB_MIGRATION:
        print(f"Unsupported schema version of DB: '{schema_version[0]}'. Supported: {LATEST_DB_MIGRATION}")
        if ignore_mismatch:
            return
        exit(1)


def get_oldest_admin_log(cur: "psycopg2.cursor") -> "datetime.datetime":
    cur.execute('SELECT "date" FROM "admin_log" ORDER BY "date" LIMIT 1')
    admin_date = cur.fetchone()
    if admin_date == None:
        print("No admin logs to read.")
        exit(0)

    return admin_date[0]


def dump_admin_in_range(cur: "psycopg2.cursor", start: "datetime.datetime", end: "datetime.datetime", outdir: str, compress: bool, delete: bool):
    date_suffix = f"{start.strftime("%Y-%m-%d")}-{end.strftime("%Y-%m-%d")}"

    # Export admin_log_player
    print("Dumping admin_log_player from {start} to {end}...")
    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        admin_log_player
    INNER JOIN
        admin_log
    ON
        admin_log_player.log_id = admin_log.admin_log_id AND admin_log.round_id = admin_log.round_id
    WHERE
        date >= %s AND date < %s
) as data
""", (start,end))

    json_data = cur.fetchall()[0][0]

    if compress:
        with open(os.path.join(outdir, f"admin_log-{date_suffix}.json"), "w", encoding="utf-8") as f:
            f.write(json_data)
    else:
        with gzip.GzipFile(os.path.join(outdir, f"admin_log-{date_suffix}.json.gz"), "w") as f:
            f.write(json_data)

    # Export admin_log
    print("Dumping admin_log from {start} to {end}...")
    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        admin_log
    WHERE
        date >= %s AND date < %s
) as data
""", (start,end))

    json_data = cur.fetchall()[0][0]

    if compress:
        with open(os.path.join(outdir, f"admin_log-{date_suffix}.json"), "w", encoding="utf-8") as f:
            f.write(json_data)
    else:
        with gzip.GzipFile(os.path.join(outdir, f"admin_log-{date_suffix}.json.gz"), "w") as f:
            f.write(json_data)
    
    if delete:
        # Delete admin_log_player
        print("Deleting admin_log_player from {start} to {end}...")
        cur.execute("""
        DELETE
        FROM
            admin_log_player
        WHERE
            (log_id, round_id)
        IN
            SELECT
                (admin_log_id, round_id)
            FROM
                admin_log
            WHERE
                date >= %s AND date < %s
        """, (start,end))

        # Delete admin_log
        print("Deleting admin_log from {start} to {end}...")
        cur.execute("""
        DELETE
        FROM
            admin_log
        WHERE
            date >= %s AND date < %s
        ) as data
        """, (start,end))


main()

