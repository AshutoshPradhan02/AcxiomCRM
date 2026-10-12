import sqlite3
from pathlib import Path

path = Path("acxiomcrm.db").resolve()
db = sqlite3.connect(path.as_uri() + "?mode=ro", uri=True)

try:
    for (table,) in db.execute("""
        SELECT name FROM sqlite_master
        WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
        ORDER BY name
    """):
        print(f"\n--- {table} ---")
        for row in db.execute(f'PRAGMA table_info("{table}")'):
            print(f"{row[1]} | {row[2]} | not_null={row[3]} | pk={row[5]}")

    print("\n--- INDEXES ---")
    for name, table, sql in db.execute("""
        SELECT name, tbl_name, sql FROM sqlite_master
        WHERE type = 'index' AND name NOT LIKE 'sqlite_%'
        ORDER BY tbl_name, name
    """):
        print(f"{table} | {name} | {sql}")
finally:
    db.close()
