#!/usr/bin/env python3
import sqlite3
import os

DB_PATH = 'faces.db'

if not os.path.exists(DB_PATH):
    print(f"❌ Baza nie znaleziona: {DB_PATH}")
    exit(1)

print(f"🔧 Naprawianie tabeli face_features...")

db = sqlite3.connect(DB_PATH)
cursor = db.cursor()

# 1. Sprawdzenie istniejącej struktury
print("\n📋 Aktualna struktura face_features:")
cursor.execute("PRAGMA table_info(face_features)")
columns = cursor.fetchall()
for col in columns:
    print(f"   - {col[1]} ({col[2]})")

# 2. Usuń starą tabelę
print("\n🗑️  Usuwanie starej tabeli...")
try:
    cursor.execute("DROP TABLE IF EXISTS face_features")
    print("   ✅ Stara tabela usunięta")
except Exception as e:
    print(f"   ⚠️  {e}")

# 3. Utwórz nową tabelę z prawidłową strukturą
print("\n✨ Tworzenie nowej tabeli...")
cursor.execute('''
    CREATE TABLE face_features (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        pesel TEXT UNIQUE NOT NULL,
        eye_color TEXT,
        hair_color TEXT,
        eye_distance REAL,
        nose_width REAL,
        mouth_width REAL,
        eyebrow_shape TEXT,
        skin_features TEXT,
        facial_asymmetry REAL,
        age_estimate TEXT,
        skin_tone TEXT,
        features_json TEXT,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (pesel) REFERENCES faces(pesel) ON DELETE CASCADE
    )
''')
print("   ✅ Nowa tabela face_features utworzona!")

# 4. Weryfikacja
print("\n✅ Nowa struktura face_features:")
cursor.execute("PRAGMA table_info(face_features)")
columns = cursor.fetchall()
for col in columns:
    print(f"   - {col[1]} ({col[2]})")

db.commit()
db.close()

print("\n✅ Naprawa zakończona!")
print("\nTeraz możesz rejestrować osoby bez błędów.")