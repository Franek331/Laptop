#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import sqlite3
import os
from config import DATABASE_PATH

print("="*60)
print("🧪 DATABASE TESTS")
print("="*60)

# Test 1: Czy baza istnieje?
print(f"\n1️⃣ Sprawdzanie bazy danych...")
print(f"   📁 Path: {DATABASE_PATH}")
print(f"   ✅ Exists: {os.path.isfile(DATABASE_PATH)}")

if not os.path.isfile(DATABASE_PATH):
    print("   ❌ Baza nie istnieje!")
    exit(1)

# Test 2: Połączenie
print(f"\n2️⃣ Łączenie się z bazą...")
try:
    conn = sqlite3.connect(DATABASE_PATH)
    cursor = conn.cursor()
    print("   ✅ Połączenie OK")
except Exception as e:
    print(f"   ❌ Błąd: {e}")
    exit(1)

# Test 3: Tabele
print(f"\n3️⃣ Sprawdzanie tabel...")
cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
tables = cursor.fetchall()
print(f"   Tabele ({len(tables)}):")
for table in tables:
    print(f"      ✅ {table[0]}")

# Test 4: Liczba osób w tabeli faces
print(f"\n4️⃣ Liczba osób w bazie (tabela faces)...")
cursor.execute("SELECT COUNT(*) FROM faces")
count = cursor.fetchone()[0]
print(f"   Total: {count}")

if count > 0:
    print(f"\n   Osoby:")
    cursor.execute("SELECT pesel, first_name, last_name FROM faces")
    for row in cursor.fetchall():
        print(f"      ✅ {row[0]}: {row[1]} {row[2]}")

# Test 5: Liczba encodingów w tabeli face_encodings
print(f"\n5️⃣ Liczba encodingów (tabela face_encodings)...")
cursor.execute("SELECT COUNT(*) FROM face_encodings")
count = cursor.fetchone()[0]
print(f"   Total: {count}")

if count > 0:
    print(f"\n   Zarejestrowane encodingi:")
    cursor.execute("SELECT pesel, model_name, created_at FROM face_encodings")
    for row in cursor.fetchall():
        print(f"      ✅ {row[0]} ({row[1]}) - {row[2]}")
else:
    print(f"   ❌ BRAK ENCODINGÓW! To jest problem!")

# Test 6: Czy PESEL z faces ma encoding w face_encodings?
print(f"\n6️⃣ Porównanie osób vs encodingów...")
cursor.execute("SELECT pesel FROM faces")
faces = [row[0] for row in cursor.fetchall()]

cursor.execute("SELECT pesel FROM face_encodings")
encodings = [row[0] for row in cursor.fetchall()]

print(f"   Osób: {len(faces)}")
print(f"   Encodingów: {len(encodings)}")

if len(faces) > 0:
    print(f"\n   Analiza:")
    for pesel in faces:
        has_encoding = pesel in encodings
        status = "✅ MA" if has_encoding else "❌ BRAK"
        print(f"      {pesel}: {status}")

conn.close()

print("\n" + "="*60)
print("✅ TESTY ZAKOŃCZONE")
print("="*60)