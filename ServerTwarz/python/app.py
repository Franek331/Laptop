#!/usr/bin/env python3
# -*- coding: utf-8 -*-

from flask import Flask, request, jsonify
from flask_cors import CORS
import os
import json
from werkzeug.utils import secure_filename

from config import UPLOADS_DIR, FEATURE_EXTRACTION_ENABLED
from face_recognition import FaceRecognizer
from utils import (
    init_db,
    get_person_by_pesel,
    get_all_people,
    get_face_encoding,
    get_all_face_encodings,
    get_face_features,
    get_all_face_features,
    get_statistics,
)

# Inicjalizacja Flask
app = Flask(__name__)
CORS(app)

# Konfiguracja
app.config['MAX_CONTENT_LENGTH'] = 16 * 1024 * 1024  # 16MB max
app.config['UPLOAD_FOLDER'] = UPLOADS_DIR

# Inicjalizuj bazę danych
init_db()

# Inicjalizuj rozpoznawacz
recognizer = FaceRecognizer()


# ✅ ENDPOINT 1: Health Check
@app.route('/health', methods=['GET'])
def health():
    """
    Status serwisu rozpoznawania twarzy
    """
    stats = get_statistics()
    return jsonify({
        "status": "ok",
        "service": "Advanced Face Recognition API v2.0",
        "primary_model": recognizer.models[0]['name'],
        "secondary_model": recognizer.models[1]['name'],
        "tertiary_model": recognizer.models[2]['name'],
        "feature_analysis": FEATURE_EXTRACTION_ENABLED,
        "database": stats
    }), 200


# ✅ ENDPOINT 2: Rejestracja twarzy
@app.route('/api/register-face-encoding', methods=['POST'])
def register_face_encoding():
    """
    Rejestruje encoding twarzy + cechy szczególne dla danego PESEL
    
    Request:
    {
        "pesel": "12345678901",
        "photo_path": "/uploads/photo.jpg"
    }
    
    ⚠️ WAŻNE: Ta osoba MUSI już być w bazie danych w tabeli 'faces'
    (Node.js API musi ją najpierw zarejestrować)
    """
    try:
        data = request.get_json()
        pesel = data.get('pesel')
        photo_path = data.get('photo_path')

        print(f"\n{'=' * 70}")
        print(f"📝 REGISTER FACE ENCODING ENDPOINT")
        print(f"{'=' * 70}")
        print(f"PESEL: {pesel}")
        print(f"Photo Path: {photo_path}")

        if not pesel or not photo_path:
            print(f"❌ Missing parameters!")
            return jsonify({
                "Sukces": False,
                "Wiadomosc": "Brakuje pesel lub photo_path"
            }), 400

        print(f"🔍 Looking for person in database (faces table)...")
        person = get_person_by_pesel(pesel)

        if not person:
            print(f"❌ Person not found in database!")
            print(f"   ⚠️ Osoba musi być najpierw zarejestrowana w Node.js API!")
            return jsonify({
                "Sukces": False,
                "Wiadomosc": "Osoba nie znaleziona w bazie. Zarejestruj ją najpierw w aplikacji!"
            }), 404

        print(f"✅ Person found: {person['first_name']} {person['last_name']}")

        print(f"🧠 Registering face with advanced analysis...")
        success = recognizer.register_person(pesel, photo_path)

        if success:
            print(f"✅ SUCCESS! Encoding + Features registered for {pesel}")
            
            # Pobierz cechy
            features = get_face_features(pesel)
            features_summary = {}
            if features:
                features_summary = {
                    'eye_color': features.get('eye_color', {}).get('name', 'unknown') if isinstance(features.get('eye_color'), dict) else features.get('eye_color', 'unknown'),
                    'hair_color': features.get('hair_color', {}).get('name', 'unknown') if isinstance(features.get('hair_color'), dict) else features.get('hair_color', 'unknown'),
                    'eye_distance': round(features.get('eye_distance', {}).get('normalized_distance', 0), 3) if isinstance(features.get('eye_distance'), dict) else 0,
                    'skin_tone': features.get('skin_tone', {}).get('skin_tone', 'unknown') if isinstance(features.get('skin_tone'), dict) else features.get('skin_tone', 'unknown')
                }
            
            return jsonify({
                "Sukces": True,
                "Pesel": pesel,
                "Imie": person['first_name'],
                "Nazwisko": person['last_name'],
                "Model": recognizer.models[0]['name'],
                "CechySzczegoly": features_summary,
                "Wiadomosc": f"Encoding + cechy zarejestrowane dla: {person['first_name']} {person['last_name']}"
            }), 200
        else:
            print(f"❌ FAILED! Could not extract encoding")
            return jsonify({
                "Sukces": False,
                "Wiadomosc": "Nie można wyciągnąć encodingu z zdjęcia"
            }), 500

    except Exception as e:
        print(f"❌ Exception: {str(e)}")
        import traceback
        traceback.print_exc()
        return jsonify({
            "Sukces": False,
            "Wiadomosc": f"Błąd: {str(e)}"
        }), 500


# ✅ ENDPOINT 3: Rozpoznaj twarz ze zdjęcia
@app.route('/api/recognize-face', methods=['POST'])
def recognize_face():
    """
    Rozpoznaje twarz na zdjęciu z wielomodelową analizą + cechy
    
    ⚠️ WAŻNE: To szuka tylko w encodingach z face_encodings
    (osób które zostały przetworzane przez Python)
    """
    try:
        data = request.get_json()
        photo_path = data.get('photo_path')

        if not photo_path:
            return jsonify({
                "Rozpoznano": False,
                "Wiadomosc": "Brakuje photo_path"
            }), 400

        print(f"\n{'=' * 70}")
        print(f"🔍 RECOGNIZE FACE ENDPOINT")
        print(f"{'=' * 70}")
        print(f"Photo Path: {photo_path}")

        result = recognizer.recognize_face(photo_path)

        # Formatuj wynik
        response = {
            "Rozpoznano": result.get("Rozpoznano", False),
            "Wiadomosc": result.get("Wiadomosc", "")
        }

        if result.get("Rozpoznano"):
            response.update({
                "Pesel": result.get("Pesel"),
                "Imie": result.get("Imie"),
                "Nazwisko": result.get("Nazwisko"),
                "DataUrodzenia": result.get("DataUrodzenia"),
                "Plec": result.get("Plec"),
                "Pewnosc": round(result.get("Pewnosc", 0), 4),
                "CechyWynik": round(result.get("CechyWynik", 0), 4),
                "WynikPolaczony": round(result.get("WynikPolaczony", 0), 4),
                "Model": result.get("Model"),
                "Dystans": round(result.get("Dystans", 0), 4),
                "SzczegolyCech": result.get("SzczegolyCech", {})
            })

        return jsonify(response), 200

    except Exception as e:
        print(f"❌ Error in recognize_face: {str(e)}")
        import traceback
        traceback.print_exc()
        return jsonify({
            "Rozpoznano": False,
            "Wiadomosc": f"Błąd: {str(e)}"
        }), 500


# ✅ ENDPOINT 4: Info o API
@app.route('/api/info', methods=['GET'])
def info():
    """
    Informacje o serwisie
    """
    stats = get_statistics()
    return jsonify({
        "name": "🧠 Advanced Face Recognition Service v2.0",
        "version": "2.0.0",
        "purpose": "Face encoding extraction and recognition (Python backend for Node.js API)",
        "models": {
            "primary": recognizer.models[0]['name'],
            "secondary": recognizer.models[1]['name'],
            "tertiary": recognizer.models[2]['name']
        },
        "features": {
            "enabled": FEATURE_EXTRACTION_ENABLED,
            "analyzed": ["eye_color", "hair_color", "eye_distance", "nose_width", "mouth_width", "eyebrow_shape", "facial_asymmetry", "age_estimate", "skin_tone"]
        },
        "database_stats": stats,
        "⚠️_IMPORTANT": "This API works with Node.js API. Persons must be registered in Node.js first!",
        "endpoints": {
            "health": "GET /health",
            "info": "GET /api/info",
            "register_encoding": "POST /api/register-face-encoding (after Node.js registration!)",
            "recognize": "POST /api/recognize-face"
        }
    }), 200


# ✅ ENDPOINT 5: 404
@app.errorhandler(404)
def not_found(error):
    return jsonify({
        "error": "Endpoint not found",
        "message": "Użyj GET /api/info aby zobaczyć dostępne endpointy"
    }), 404


# 🚀 Start serwera
if __name__ == '__main__':
    print("\n" + "=" * 70)
    print("🧠 Advanced Face Recognition Service v2.0")
    print("=" * 70)
    print(f"📍 Running on: http://localhost:5001")
    print(f"🌐 CORS enabled")
    print(f"🎯 Primary Model: {recognizer.models[0]['name']}")
    print(f"👁️  Feature Analysis: {'ENABLED' if FEATURE_EXTRACTION_ENABLED else 'DISABLED'}")
    print(f"\n⚠️  ВАЖНО: Ten API pracuje z Node.js API!")
    print(f"   1. Node.js: Rejestruj osobę (POST /api/register)")
    print(f"   2. Python: Wyciągnij encoding (POST /api/register-face-encoding)")
    print(f"   3. Python: Rozpoznaj twarz (POST /api/recognize-face)")
    print("=" * 70 + "\n")

    app.run(
        host='0.0.0.0',
        port=5001,
        debug=True,
        use_reloader=False
    )