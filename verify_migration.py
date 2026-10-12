import sqlite3
from pathlib import Path

path = Path("AcxiomCRM.Web/acxiomcrm_migration_test.db").resolve()
print("Database exists:", path.exists())
print("Database path:", path)

if path.exists():
    db = sqlite3.connect(path.as_uri() + "?mode=ro", uri=True)
    try:
        print("Tables:", db.execute(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table'"
        ).fetchone()[0])
        print("Applied migrations:", db.execute(
            'SELECT MigrationId FROM "__EFMigrationsHistory"'
        ).fetchall())
    finally:
        db.close()
