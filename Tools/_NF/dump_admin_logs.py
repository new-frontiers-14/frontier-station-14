#!/usr/bin/env python3

# Admin log dump script 
# Dumps existing logs, optionally compressed, and optionally deletes them

import argparse
import os
import psycopg2
import gzip
import datetime
import calendar
import json

LATEST_DB_MIGRATION = "20250211131517_LoadoutNames"

def main():
    parser = argparse.ArgumentParser(description="Dumps admin logs into files by months and optionally deletes them from a postgres DB.")
    parser.add_argument("out_dir", help="Directory to output data dumps into.")
    parser.add_argument("--date", help="Date to save/remove info until, must be in ISO format - time zone if unspecified will be UTC. Defaults to midnight, UTC, on the beginning of the month, 6 calendar months ago.")
    parser.add_argument("--compress", action="store_true", help="If set, compresses the contents of the file in .gzip format.")
    parser.add_argument("--delete", action="store_true", help="If set, deletes the contents of the tables after writing the output.")
    parser.add_argument("--ignore-schema-mismatch", action="store_true", help="If set, ignores that the DB does not match the expected schema.")
    parser.add_argument("--connection-string", required=True, help="Database connection string to use. See https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING")

    args = parser.parse_args()

    arg_output: str = args.out_dir

    if not os.path.exists(arg_output):
        print(f"Creating output directory {arg_output} (doesn't exist yet)")
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
    print(f"Exporting {compressed_string} admin logs until {end_date}.")

    conn = psycopg2.connect(args.connection_string)
    cur = conn.cursor()

    # Find oldest dated entry - hack: discard time zone info
    oldest_record = get_oldest_admin_log(cur)
    oldest_record = oldest_record.astimezone(None)

    # From this, create your intervals up to the deleted time.
    if oldest_record > end_date:
        print(f"Nothing to export. Oldest record {oldest_record} is older than given date {end_date}.")
        return

    first_record_time = datetime.datetime(oldest_record.year, oldest_record.month, oldest_record.day, tzinfo=datetime.timezone.utc)
    old_date = first_record_time
    months_to_add = 1

    while old_date < end_date:
        new_date = add_months(first_record_time, months_to_add)
        if new_date > end_date:
            new_date = end_date

        dump_admin_in_range(cur, old_date, new_date, arg_output, args.compress, args.delete)

        # Ensure modifications go through (or if not deleting, that temp table is destroyed)
        conn.commit()

        old_date = new_date
        months_to_add += 1

# Taken from https://stackoverflow.com/questions/4130922/ (thank you, David Webb)
def add_months(date_in: "datetime.datetime", months: int) -> datetime.datetime:
    month = date_in.month - 1 + months
    year = date_in.year + date_in.month // 12
    month = date_in.month % 12 + 1
    day = min(date_in.day, calendar.monthrange(year, month)[1])
    return datetime.datetime(year, month, day, tzinfo=datetime.timezone.utc)

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
    date_suffix = f"{start.strftime('%Y%m%d')}-{end.strftime('%Y%m%d')}"
    print() # Newline

    # Create a temp table for our admin log rows of interest, make sure it drops on commit.
    cur.execute("""
CREATE TEMP TABLE admin_dump
ON COMMIT DROP
AS
    (SELECT
        admin_log_id, round_id
    FROM
        admin_log
    WHERE
        date >= %s AND date < %s
    )
    """, (start, end))

    # Export admin_log_player
    print(f"Dumping admin_log_player from {start.date()} to {end.date()}...")

    if compress:
        file_obj = gzip.GzipFile(os.path.join(outdir, f"admin_log_player-{date_suffix}.json.gz"), "w")
    else:
        file_obj = open(os.path.join(outdir, f"admin_log_player-{date_suffix}.json"), "w")
    
    file_obj.write("[".encode("utf-8"))

    cur.execute("""
SELECT
    json_agg(to_jsonb(alp.*))
FROM
    admin_log_player alp JOIN admin_dump ad
ON
    alp.log_id = ad.admin_log_id AND alp.round_id = ad.round_id
GROUP BY alp.round_id, alp.log_id
ORDER BY alp.round_id, alp.log_id
    """)

    first_row = True
    while True:
        data = cur.fetchmany(500)
        if len(data) <= 0:
            break

        for row in data:
            # Strip braces off content, add a comma if we're writing to the same file.
            if not first_row:
                file_obj.write(", ".encode('utf-8'))
            else:
                first_row = False
            file_obj.write(json.dumps(row[0][0]).encode('utf-8'))

    file_obj.write("]".encode("utf-8"))

    # Export admin_log
    offset = 0
    more_rows = True

    if compress:
        file_obj = gzip.GzipFile(os.path.join(outdir, f"admin_log-{date_suffix}.json.gz"), "w")
    else:
        file_obj = open(os.path.join(outdir, f"admin_log-{date_suffix}.json"), "w")
    
    file_obj.write("[".encode("utf-8"))

    print(f"Dumping admin_log from {start.date()} to {end.date()}...")
    cur.execute("""
SELECT
    json_agg(to_jsonb(al.*))
FROM
    admin_log al JOIN admin_dump ad
ON
    al.admin_log_id = ad.admin_log_id AND al.round_id = ad.round_id
GROUP BY al.round_id, al.admin_log_id
ORDER BY al.round_id, al.admin_log_id
    """)

    first_row = True
    while True:
        data = cur.fetchmany(500)
        if len(data) <= 0:
            break

        # Strip braces off content, add a comma if we're writing to the same file.
        for row in data:
            if not first_row:
                file_obj.write(", ".encode('utf-8'))
            else:
                first_row = False
            file_obj.write(json.dumps(row[0][0]).encode('utf-8'))

    file_obj.write("]".encode("utf-8"))

    if delete:
        # Delete admin_log_player
        print(f"Deleting admin_log_player from {start.date()} to {end.date()}...")
        cur.execute("""
DELETE FROM
    admin_log_player alp
USING
    admin_dump ad
WHERE
    alp.log_id = ad.admin_log_id AND alp.round_id = ad.round_id
        """)

        # Delete admin_log
        print(f"Deleting admin_log from {start.date()} to {end.date()}...")
        cur.execute("""
DELETE FROM
    admin_log
WHERE
    date < %s
        """, (end,))


main()
