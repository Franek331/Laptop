#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import sqlite3
import json
import os
from config import DATABASE_PATH

# ═══════════════════════════════════════════════════════════════════════════
# 🗄️  DATABASE UTILITIES - KOMPATYBILNE Z NODE.JS
# ═══════════════════════════════════════════════════════════════════════════

def get_db():
    """Pobierz połączenie do bazy danych"""
    db = sqlite3.connect(DATABASE_PATH)
    db.row_factory = sqlite3.Row
    return db

def init_db():
    """
    Inicjalizuj bazę danych - UŻYWAJ TEJ SAMEJ STRUKTURY CO NODE.JS!
    
    ⚠️ WAŻNE: Node.js używa tabeli 'faces' - Python musi też!
    """
    try:
        db = get_db()
        cursor = db.cursor()

        print(f"\n{'='*70}")
        print(f"🗄️  INITIALIZING DATABASE (NODE.JS COMPATIBLE)")
        print(f"{'='*70}")

        # ✅ TABELA: faces (TAKA SAMA JAK W NODE.JS!)
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS faces (
                pesel TEXT PRIMARY KEY,
                first_name TEXT NOT NULL,
                last_name TEXT NOT NULL,
                date_of_birth DATE,
                gender TEXT,
                embedding TEXT,
                photo_path TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        print("✅ Tabela 'faces' gotowa (kompatybilna z Node.js)")

        # ✅ TABELA: face_encodings (dla python - zawiera encoding z Facenet)
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS face_encodings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pesel TEXT UNIQUE NOT NULL,
                encoding TEXT NOT NULL,
                model_name TEXT DEFAULT 'Facenet',
                dimensions INTEGER DEFAULT 128,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (pesel) REFERENCES faces(pesel) ON DELETE CASCADE
            )
        ''')
        print("✅ Tabela 'face_encodings' gotowa (Python encodingi)")

        # ✅ TABELA: face_features (dla python - cechy szczególne)
        cursor.execute('''
            CREATE TABLE IF NOT EXISTS face_features (
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
        print("✅ Tabela 'face_features' gotowa (Python cechy)")

        db.commit()
        db.close()

        print(f"{'='*70}")
        print(f"✅ Baza danych gotowa!")
        print(f"{'='*70}\n")
        return True

    except Exception as e:
        print(f"❌ Error initializing database: {str(e)}")
        return False

# ═══════════════════════════════════════════════════════════════════════════
# 👤 PERSON FUNCTIONS - KOMPATYBILNE Z NODE.JS
# ═══════════════════════════════════════════════════════════════════════════

def get_person_by_pesel(pesel: str) -> dict:
    """
    Pobierz osobę po PESEL ze tabeli 'faces' (jak Node.js)
    """
    try:
        db = get_db()
        cursor = db.cursor()

        cursor.execute(
            'SELECT * FROM faces WHERE pesel = ?',
            (pesel,)
        )

        row = cursor.fetchone()
        db.close()

        if row:
            return {
                'pesel': row['pesel'],
                'first_name': row['first_name'],
                'last_name': row['last_name'],
                'date_of_birth': row['date_of_birth'],
                'gender': row['gender'],
                'photo_path': row['photo_path'],
                'created_at': row['created_at']
            }
        return None

    except Exception as e:
        print(f"❌ Error getting person: {str(e)}")
        return None

def get_all_people() -> list:
    """Pobierz wszystkie osoby ze tabeli 'faces'"""
    try:
        db = get_db()
        cursor = db.cursor()

        cursor.execute('SELECT * FROM faces')
        rows = cursor.fetchall()
        db.close()

        return [dict(row) for row in rows]

    except Exception as e:
        print(f"❌ Error getting all people: {str(e)}")
        return []

# ═══════════════════════════════════════════════════════════════════════════
# 🧠 FACE ENCODING FUNCTIONS - PYTHON ENCODINGI (128-dim)
# ═══════════════════════════════════════════════════════════════════════════

def save_face_encoding(pesel: str, encoding: list, model_name: str = 'Facenet') -> bool:
    """
    Zapisz encoding twarzy do tabeli 'face_encodings'
    
    ⚠️ WAŻNE: To przechowuje encoding z Facenet (128 wymiarów)
    Node.js embedding w tabeli 'faces' zostaje niezmieniony
    
    Args:
        pesel: PESEL osoby
        encoding: lista liczb (128-wymiarowy wektor)
        model_name: nazwa modelu (zawsze 'Facenet')
    """
    try:
        db = get_db()
        cursor = db.cursor()

        # Konwertuj encoding na JSON
        encoding_json = json.dumps(encoding)
        dimensions = len(encoding)

        print(f"   💾 Saving encoding to face_encodings for {pesel}")
        print(f"      Model: {model_name}, Dimensions: {dimensions}")

        # Sprawdź czy encoding już istnieje
        cursor.execute(
            'SELECT id FROM face_encodings WHERE pesel = ?',
            (pesel,)
        )

        existing = cursor.fetchone()

        if existing:
            # Update istniejącego
            cursor.execute('''
                UPDATE face_encodings 
                SET encoding = ?, model_name = ?, dimensions = ?
                WHERE pesel = ?
            ''', (encoding_json, model_name, dimensions, pesel))
            print(f"   ✅ Encoding UPDATED for {pesel}")
        else:
            # Wstaw nowy
            cursor.execute('''
                INSERT INTO face_encodings (pesel, encoding, model_name, dimensions)
                VALUES (?, ?, ?, ?)
            ''', (pesel, encoding_json, model_name, dimensions))
            print(f"   ✅ Encoding INSERTED for {pesel}")

        db.commit()
        db.close()
        return True

    except Exception as e:
        print(f"❌ Error saving encoding: {str(e)}")
        import traceback
        traceback.print_exc()
        return False

def get_face_encoding(pesel: str) -> list:
    """Pobierz encoding twarzy dla osoby z tabeli 'face_encodings'"""
    try:
        db = get_db()
        cursor = db.cursor()

        cursor.execute(
            'SELECT encoding FROM face_encodings WHERE pesel = ?',
            (pesel,)
        )

        row = cursor.fetchone()
        db.close()

        if row:
            return json.loads(row['encoding'])
        return None

    except Exception as e:
        print(f"❌ Error getting encoding: {str(e)}")
        return None

def get_all_face_encodings() -> dict:
    """
    Pobierz wszystkie encodingi z tabeli 'face_encodings'
    Zwraca: {pesel: encoding_list}
    """
    try:
        db = get_db()
        cursor = db.cursor()

        cursor.execute('SELECT pesel, encoding FROM face_encodings')
        rows = cursor.fetchall()
        db.close()

        result = {}
        for row in rows:
            result[row['pesel']] = json.loads(row['encoding'])

        print(f"   📊 Loaded {len(result)} encodings from face_encodings table")
        return result

    except Exception as e:
        print(f"❌ Error getting all encodings: {str(e)}")
        return {}

# ═══════════════════════════════════════════════════════════════════════════
# 👁️  FACE FEATURES FUNCTIONS
# ═══════════════════════════════════════════════════════════════════════════

def save_face_features(pesel: str, features: dict) -> bool:
    """
    Zapisz cechy szczególne twarzy do tabeli 'face_features'
    """
    try:
        db = get_db()
        cursor = db.cursor()

        # Konwertuj features na JSON
        features_json = json.dumps(features)

        # Wyciągnij konkretne pola
        eye_color = features.get('eye_color', {}).get('name', 'unknown') if isinstance(features.get('eye_color'), dict) else features.get('eye_color', 'unknown')
        hair_color = features.get('hair_color', {}).get('name', 'unknown') if isinstance(features.get('hair_color'), dict) else features.get('hair_color', 'unknown')
        eye_distance = features.get('eye_distance', {}).get('normalized_distance', 0) if isinstance(features.get('eye_distance'), dict) else 0
        nose_width = features.get('nose_width', {}).get('width_estimate', 0) if isinstance(features.get('nose_width'), dict) else 0
        mouth_width = features.get('mouth_width', {}).get('aspect_ratio', 0) if isinstance(features.get('mouth_width'), dict) else 0
        eyebrow_shape = features.get('eyebrow_shape', {}).get('shape_description', 'unknown') if isinstance(features.get('eyebrow_shape'), dict) else features.get('eyebrow_shape', 'unknown')
        skin_features = features.get('skin_features', {}).get('texture_description', 'unknown') if isinstance(features.get('skin_features'), dict) else 'unknown'
        facial_asymmetry = features.get('facial_asymmetry', {}).get('asymmetry_score', 0) if isinstance(features.get('facial_asymmetry'), dict) else 0
        age_estimate = features.get('age_estimate', {}).get('estimated_age_group', 'unknown') if isinstance(features.get('age_estimate'), dict) else 'unknown'
        skin_tone = features.get('skin_tone', {}).get('skin_tone', 'unknown') if isinstance(features.get('skin_tone'), dict) else features.get('skin_tone', 'unknown')

        # Sprawdź czy features już istnieją
        cursor.execute(
            'SELECT id FROM face_features WHERE pesel = ?',
            (pesel,)
        )

        existing = cursor.fetchone()

        if existing:
            # Update istniejących
            cursor.execute('''
                UPDATE face_features 
                SET eye_color = ?, hair_color = ?, eye_distance = ?, nose_width = ?,
                    mouth_width = ?, eyebrow_shape = ?, skin_features = ?,
                    facial_asymmetry = ?, age_estimate = ?, skin_tone = ?, features_json = ?
                WHERE pesel = ?
            ''', (eye_color, hair_color, eye_distance, nose_width, mouth_width,
                  eyebrow_shape, skin_features, facial_asymmetry, age_estimate, skin_tone,
                  features_json, pesel))
            print(f"   ✅ Features UPDATED for {pesel}")
        else:
            # Wstaw nowe
            cursor.execute('''
                INSERT INTO face_features 
                (pesel, eye_color, hair_color, eye_distance, nose_width, mouth_width,
                 eyebrow_shape, skin_features, facial_asymmetry, age_estimate, skin_tone, features_json)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (pesel, eye_color, hair_color, eye_distance, nose_width, mouth_width,
                  eyebrow_shape, skin_features, facial_asymmetry, age_estimate, skin_tone,
                  features_json))
            print(f"   ✅ Features INSERTED for {pesel}")

        db.commit()
        db.close()
        return True

    except Exception as e:
        print(f"❌ Error saving features: {str(e)}")
        import traceback
        traceback.print_exc()
        return False

def get_face_features(pesel: str) -> dict:
    """Pobierz cechy szczególne dla osoby"""
    try:
        db = get_db()
        cursor = db.cursor()

        cursor.execute(
            'SELECT features_json FROM face_features WHERE pesel = ?',
            (pesel,)
        )

        row = cursor.fetchone()
        db.close()

        if row and row['features_json']:
            return json.loads(row['features_json'])
        return None

    except Exception as e:
        print(f"❌ Error getting features: {str(e)}")
        return None

def get_all_face_features() -> dict:
    """Pobierz wszystkie cechy jako słownik {pesel: features}"""
    try:
        db = get_db()
        cursor = db.cursor()

        cursor.execute('SELECT pesel, features_json FROM face_features WHERE features_json IS NOT NULL')
        rows = cursor.fetchall()
        db.close()

        result = {}
        for row in rows:
            if row['features_json']:
                result[row['pesel']] = json.loads(row['features_json'])

        return result

    except Exception as e:
        print(f"❌ Error getting all features: {str(e)}")
        return {}

# ═══════════════════════════════════════════════════════════════════════════
# 📚 UTILITY FUNCTIONS
# ═══════════════════════════════════════════════════════════════════════════

def file_exists(file_path: str) -> bool:
    """Sprawdź czy plik istnieje"""
    return os.path.isfile(file_path)

def get_full_path(path: str) -> str:
    """Pobierz pełną ścieżkę do pliku"""
    if path.startswith('/uploads/'):
        # Konwertuj relative path na absolute
        return os.path.abspath(
            os.path.join(
                os.path.dirname(__file__),
                '../uploads/',
                path.replace('/uploads/', '')
            )
        )
    elif path.startswith('/'):
        return path
    else:
        return os.path.abspath(
            os.path.join(os.path.dirname(__file__), '..', path)
        )

def get_statistics() -> dict:
    """Pobierz statystyki bazy danych"""
    try:
        db = get_db()
        cursor = db.cursor()

        # Liczba osób
        cursor.execute('SELECT COUNT(*) as count FROM faces')
        total_persons = cursor.fetchone()['count']

        # Liczba encodingów
        cursor.execute('SELECT COUNT(*) as count FROM face_encodings')
        total_encodings = cursor.fetchone()['count']

        # Liczba cech
        cursor.execute('SELECT COUNT(*) as count FROM face_features')
        total_features = cursor.fetchone()['count']

        db.close()

        return {
            'total_persons': total_persons,
            'total_encodings': total_encodings,
            'total_features': total_features,
            'coverage': {
                'encodings': f"{(total_encodings/total_persons*100):.1f}%" if total_persons > 0 else "0%",
                'features': f"{(total_features/total_persons*100):.1f}%" if total_persons > 0 else "0%"
            }
        }

    except Exception as e:
        print(f"❌ Error getting statistics: {str(e)}")
        return {}

print("✅ utils.py loaded (Node.js compatible)")