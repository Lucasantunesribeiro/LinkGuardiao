import os
import sqlite3
import uuid
from datetime import datetime, timezone

import boto3


def to_iso(value):
    if value is None:
        return None
    if isinstance(value, str):
        return value
    if isinstance(value, (int, float)):
        return datetime.fromtimestamp(value, tz=timezone.utc).isoformat()
    if isinstance(value, datetime):
        if value.tzinfo is None:
            value = value.replace(tzinfo=timezone.utc)
        return value.isoformat()
    return str(value)


def to_epoch_seconds(value):
    if value is None:
        return None
    if isinstance(value, (int, float)):
        return int(value)
    if isinstance(value, datetime):
        if value.tzinfo is None:
            value = value.replace(tzinfo=timezone.utc)
        return int(value.timestamp())
    if isinstance(value, str):
        try:
            return int(datetime.fromisoformat(value).replace(tzinfo=timezone.utc).timestamp())
        except ValueError:
            return None
    return None


def main():
    sqlite_path = os.environ.get("SQLITE_PATH")
    if not sqlite_path:
        raise SystemExit("Set SQLITE_PATH to the existing SQLite database file.")

    links_table_name = os.environ.get("DDB_TABLE_LINKS")
    users_table_name = os.environ.get("DDB_TABLE_USERS")
    access_table_name = os.environ.get("DDB_TABLE_ACCESS")

    if not links_table_name or not users_table_name:
        raise SystemExit("Set DDB_TABLE_LINKS and DDB_TABLE_USERS before running.")

    session = boto3.Session(region_name=os.environ.get("AWS_REGION", "us-east-1"))
    dynamodb = session.resource("dynamodb")
    links_table = dynamodb.Table(links_table_name)
    users_table = dynamodb.Table(users_table_name)
    access_table = dynamodb.Table(access_table_name) if access_table_name else None

    conn = sqlite3.connect(sqlite_path)
    conn.row_factory = sqlite3.Row

    cursor = conn.cursor()
    cursor.execute("SELECT Id, Username, Email, PasswordHash, CreatedAt, IsAdmin FROM Users")
    users = cursor.fetchall()

    user_id_map = {}
    for row in users:
        new_id = uuid.uuid4().hex
        user_id_map[row["Id"]] = new_id
        users_table.put_item(
            Item={
                "userId": new_id,
                "email": (row["Email"] or "").strip().lower(),
                "username": row["Username"] or "",
                "passwordHash": row["PasswordHash"] or "",
                "createdAt": to_iso(row["CreatedAt"]) or datetime.now(timezone.utc).isoformat(),
                "isAdmin": bool(row["IsAdmin"]),
            }
        )

    cursor.execute(
        "SELECT Id, UserId, OriginalUrl, ShortCode, Title, PasswordHash, CreatedAt, ExpiresAt, IsActive, ClickCount FROM ShortenedLinks"
    )
    links = cursor.fetchall()

    link_id_map = {}
    for row in links:
        short_code = row["ShortCode"]
        link_id_map[row["Id"]] = short_code
        expires_at = to_iso(row["ExpiresAt"])
        expires_epoch = to_epoch_seconds(expires_at)
        item = {
            "shortCode": short_code,
            "userId": user_id_map.get(row["UserId"], "unknown"),
            "originalUrl": row["OriginalUrl"] or "",
            "createdAt": to_iso(row["CreatedAt"]) or datetime.now(timezone.utc).isoformat(),
            "isActive": bool(row["IsActive"]),
            "clickCount": int(row["ClickCount"] or 0),
        }
        if row["Title"]:
            item["title"] = row["Title"]
        if row["PasswordHash"]:
            item["passwordHash"] = row["PasswordHash"]
        if expires_at and expires_epoch:
            item["expiresAt"] = expires_at
            item["expiresAtEpoch"] = expires_epoch

        links_table.put_item(Item=item)

    if access_table is not None:
        cursor.execute(
            "SELECT ShortenedLinkId, IpAddress, UserAgent, ReferrerUrl, Browser, OperatingSystem, DeviceType, AccessTime FROM LinkAccesses"
        )
        accesses = cursor.fetchall()
        for row in accesses:
            short_code = link_id_map.get(row["ShortenedLinkId"])
            if not short_code:
                continue
            access_time = to_iso(row["AccessTime"]) or datetime.now(timezone.utc).isoformat()
            item = {
                "shortCode": short_code,
                "accessTime": access_time,
                "ipAddress": row["IpAddress"] or "",
                "userAgent": row["UserAgent"] or "",
                "referrerUrl": row["ReferrerUrl"] or "",
                "browser": row["Browser"] or "",
                "operatingSystem": row["OperatingSystem"] or "",
                "deviceType": row["DeviceType"] or "",
            }
            access_table.put_item(Item=item)

    conn.close()
    print("Migration finished.")


if __name__ == "__main__":
    main()
