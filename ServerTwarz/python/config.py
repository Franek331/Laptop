#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import os

# 🔧 Konfiguracja ścieżek
DATABASE_PATH = os.path.join(os.path.dirname(__file__), '../faces.db')
UPLOADS_DIR = os.path.join(os.path.dirname(__file__), '../uploads')
ENCODINGS_DIR = os.path.join(os.path.dirname(__file__), '../encodings')

# ═══════════════════════════════════════════════════════════════════════════
# ⭐ KONFIGURACJA WIELOMODELOWA - ROZPOZNAWANIE TWARZY
# ═══════════════════════════════════════════════════════════════════════════
# 
# PROBLEM: Różne modele DeepFace mają RÓŻNE wymiary encodingów
#   - Facenet:   128 wymiarów ✅
#   - OpenFace:  128 wymiarów ✅
#   - ArcFace:   512 wymiarów ❌ (INCOMPATIBLE)
#   - VGGFace2:  2048 wymiarów ❌ (INCOMPATIBLE)
#   - DeepID:    160 wymiarów ❌ (INCOMPATIBLE)
#
# ROZWIĄZANIE: Używamy TYLKO modeli z 128 wymiarami!
# ═══════════════════════════════════════════════════════════════════════════

# ✅ GŁÓWNY MODEL - Facenet (128 wymiarów, szybki i dokładny)
DEEPFACE_PRIMARY_MODEL = 'Facenet'
DEEPFACE_PRIMARY_THRESHOLD = 0.40      # Surowy i dokładny (0.40 = conservative)
PRIMARY_DIMENSIONS = 128

# ✅ MODELE ZAPASOWE - różne progi dla tego samego wymiaru
DEEPFACE_SECONDARY_MODEL = 'OpenFace'  # Alternatywa - też 128 wymiarów
DEEPFACE_SECONDARY_THRESHOLD = 0.45    # Nieco bardziej liberalny
SECONDARY_DIMENSIONS = 128

# ✅ TERCIARY MODEL - fallback w razie potrzeby
DEEPFACE_TERTIARY_MODEL = 'Facenet'    # Ponownie Facenet, ale z wyższym progiem
DEEPFACE_TERTIARY_THRESHOLD = 0.50     # Najbardziej liberalny
TERTIARY_DIMENSIONS = 128

# ═══════════════════════════════════════════════════════════════════════════
# ⭐ ANALIZA CECH SZCZEGÓLNYCH - WZMOCNIE ROZPOZNAWANIE
# ═══════════════════════════════════════════════════════════════════════════

FEATURE_EXTRACTION_ENABLED = True
FEATURE_THRESHOLD_MATCH = 0.75          # Próg dla cech szczególnych (0-1)

# Wagi dla różnych cech (suma musi być = 1.0 lub ≤ 1.0)
FEATURE_WEIGHTS = {
    'eye_color': 0.20,                  # 20% - Kolor oczu
    'hair_color': 0.15,                 # 15% - Kolor włosów
    'eye_distance': 0.15,               # 15% - Rozstaw między oczami
    'nose_width': 0.12,                 # 12% - Szerokość nosa
    'mouth_width': 0.10,                # 10% - Szerokość ust
    'eyebrow_shape': 0.10,              # 10% - Kształt brwi
    'facial_asymmetry': 0.08,           # 8%  - Asymetria twarzy
    'skin_tone': 0.10                   # 10% - Ton skóry
}
# SUMA: 1.0 ✅

# ═══════════════════════════════════════════════════════════════════════════
# ⭐ OPCJE DETEKTORA TWARZY
# ═══════════════════════════════════════════════════════════════════════════

DETECTOR_BACKEND = 'retinaface'         # Najlepszy: 'retinaface', 'yolov8', 'mtcnn'
DETECTOR_ENFORCE = False                # Pozwól na słabsze detektory jeśli potrzeba
GPU_ENABLED = True                      # Użyj GPU jeśli dostępne

# ═══════════════════════════════════════════════════════════════════════════
# ⭐ API Configuration
# ═══════════════════════════════════════════════════════════════════════════

API_HOST = 'http://localhost:5000'
API_TIMEOUT = 120
API_PORT = 5001

# ═══════════════════════════════════════════════════════════════════════════
# ⭐ OPCJE OPTYMALIZACJI
# ═══════════════════════════════════════════════════════════════════════════

ALLOW_FACE_DETECTION_BLUR = False       # Nie rozpoznawaj rozmytych twarzy
CACHE_ENCODINGS = True                  # Cache encodingi w pamięci
MAX_CACHE_SIZE = 1000                   # Maks encodingi w cache'u

# ═══════════════════════════════════════════════════════════════════════════
# ⭐ STRATEGIA ROZPOZNAWANIA - W KTÓREJ KOLEJNOŚCI PRÓBOWAĆ
# ═══════════════════════════════════════════════════════════════════════════
# 
# 1. Primary (Facenet, threshold=0.40)  - Najsurowszy, tylko pewne matche
# 2. Secondary (OpenFace, threshold=0.45) - Średni próg
# 3. Tertiary (Facenet, threshold=0.50) - Najbardziej liberalny, fallback
#
# Jeśli któryś model da pozytywny wynik -> zwróć to
# Jeśli żaden model nie da wyniku -> "Nie rozpoznano"
# ═══════════════════════════════════════════════════════════════════════════

RECOGNITION_STRATEGY = [
    {
        'model': DEEPFACE_PRIMARY_MODEL,
        'threshold': DEEPFACE_PRIMARY_THRESHOLD,
        'weight': 0.5,  # To jest główny model
        'name': 'Primary (Facenet - Conservative)'
    },
    {
        'model': DEEPFACE_SECONDARY_MODEL,
        'threshold': DEEPFACE_SECONDARY_THRESHOLD,
        'weight': 0.3,
        'name': 'Secondary (OpenFace - Balanced)'
    },
    {
        'model': DEEPFACE_TERTIARY_MODEL,
        'threshold': DEEPFACE_TERTIARY_THRESHOLD,
        'weight': 0.2,
        'name': 'Tertiary (Facenet - Liberal)'
    }
]

# ═══════════════════════════════════════════════════════════════════════════
# 📁 Tworzenie folderów jeśli nie istnieją
# ═══════════════════════════════════════════════════════════════════════════

os.makedirs(ENCODINGS_DIR, exist_ok=True)
os.makedirs(UPLOADS_DIR, exist_ok=True)

# ═══════════════════════════════════════════════════════════════════════════
# 📊 Info o konfiguracji
# ═══════════════════════════════════════════════════════════════════════════

if __name__ != '__main__':
    print("\n" + "=" * 80)
    print("🧠 FACE RECOGNITION CONFIGURATION LOADED")
    print("=" * 80)
    print(f"📁 Database:        {DATABASE_PATH}")
    print(f"📁 Uploads:         {UPLOADS_DIR}")
    print(f"📁 Encodings:       {ENCODINGS_DIR}")
    print(f"\n✅ PRIMARY MODEL:   {DEEPFACE_PRIMARY_MODEL} (threshold: {DEEPFACE_PRIMARY_THRESHOLD}, dims: {PRIMARY_DIMENSIONS})")
    print(f"✅ SECONDARY MODEL: {DEEPFACE_SECONDARY_MODEL} (threshold: {DEEPFACE_SECONDARY_THRESHOLD}, dims: {SECONDARY_DIMENSIONS})")
    print(f"✅ TERTIARY MODEL:  {DEEPFACE_TERTIARY_MODEL} (threshold: {DEEPFACE_TERTIARY_THRESHOLD}, dims: {TERTIARY_DIMENSIONS})")
    print(f"👁️  FEATURE ANALYSIS: {'ENABLED' if FEATURE_EXTRACTION_ENABLED else 'DISABLED'}")
    print(f"🎯 DETECTOR:        {DETECTOR_BACKEND}")
    print(f"🚀 GPU:             {'ENABLED' if GPU_ENABLED else 'DISABLED'}")
    print(f"💾 CACHE:           {'ENABLED' if CACHE_ENCODINGS else 'DISABLED'}")
    print("=" * 80 + "\n")