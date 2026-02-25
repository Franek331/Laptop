#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import cv2
import numpy as np
from deepface import DeepFace
from pathlib import Path
import os
import sys
import json
from PIL import Image
from PIL.ExifTags import TAGS

from config import (
    DEEPFACE_PRIMARY_MODEL,
    DEEPFACE_PRIMARY_THRESHOLD,
    DEEPFACE_SECONDARY_MODEL,
    DEEPFACE_SECONDARY_THRESHOLD,
    DEEPFACE_TERTIARY_MODEL,
    DEEPFACE_TERTIARY_THRESHOLD,
    PRIMARY_DIMENSIONS,
    SECONDARY_DIMENSIONS,
    TERTIARY_DIMENSIONS,
    FEATURE_EXTRACTION_ENABLED,
    FEATURE_WEIGHTS,
    FEATURE_THRESHOLD_MATCH,
    UPLOADS_DIR,
    DETECTOR_BACKEND,
    DETECTOR_ENFORCE,
    RECOGNITION_STRATEGY
)
from utils import (
    init_db,
    get_person_by_pesel,
    get_all_people,
    save_face_encoding,
    get_face_encoding,
    get_all_face_encodings,
    save_face_features,
    get_face_features,
    get_all_face_features,
    file_exists,
    get_full_path
)
from face_feature_analyzer import FaceFeatureAnalyzer


class FaceRecognizer:
    def __init__(self):
        """Inicjalizuj rozpoznawacz twarzy z wielomodelową analizą"""
        print("\n" + "=" * 70)
        print("🧠 Initializing Advanced Face Recognition System")
        print("=" * 70)

        init_db()
        self.feature_analyzer = FaceFeatureAnalyzer()

        # ⭐ MODELE - wszystkie z 128 wymiarami!
        self.models = [
            {
                'name': DEEPFACE_PRIMARY_MODEL,
                'threshold': DEEPFACE_PRIMARY_THRESHOLD,
                'dimensions': PRIMARY_DIMENSIONS
            },
            {
                'name': DEEPFACE_SECONDARY_MODEL,
                'threshold': DEEPFACE_SECONDARY_THRESHOLD,
                'dimensions': SECONDARY_DIMENSIONS
            },
            {
                'name': DEEPFACE_TERTIARY_MODEL,
                'threshold': DEEPFACE_TERTIARY_THRESHOLD,
                'dimensions': TERTIARY_DIMENSIONS
            }
        ]

        print(f"✅ Primary Model: {self.models[0]['name']} (threshold: {self.models[0]['threshold']}, dims: {self.models[0]['dimensions']})")
        print(f"✅ Secondary Model: {self.models[1]['name']} (threshold: {self.models[1]['threshold']}, dims: {self.models[1]['dimensions']})")
        print(f"✅ Tertiary Model: {self.models[2]['name']} (threshold: {self.models[2]['threshold']}, dims: {self.models[2]['dimensions']})")
        print(f"👁️  Feature Analysis: ENABLED")
        print(f"🎯 Detector: {DETECTOR_BACKEND}")
        print(f"📊 Feature Weights Configured")
        print("=" * 70 + "\n")

    @staticmethod
    def fix_image_rotation(image_path):
        """
        ⭐ Napraw rotację zdjęcia ze aparatu
        Czyta metadane EXIF i obraca obraz jeśli trzeba
        """
        try:
            print(f"🔧 Checking image rotation...")

            image = Image.open(image_path)

            # Odczytaj metadane EXIF
            exif = {}
            try:
                exif_data = image._getexif()
                if exif_data:
                    for tag_id, value in exif_data.items():
                        tag = TAGS.get(tag_id, tag_id)
                        exif[tag] = value

                    # Szukaj orientacji (tag 274)
                    orientation = exif.get('Orientation', 1)
                    print(f"   📐 EXIF Orientation: {orientation}")

                    # Obrót w zależności od orientacji
                    if orientation == 3:
                        print(f"   🔄 Rotating 180°")
                        image = image.rotate(180, expand=True)
                    elif orientation == 6:
                        print(f"   🔄 Rotating 270°")
                        image = image.rotate(270, expand=True)
                    elif orientation == 8:
                        print(f"   🔄 Rotating 90°")
                        image = image.rotate(90, expand=True)

                    # Zapisz poprawiony obraz
                    image.save(image_path)
                    print(f"   ✅ Image rotation fixed")

            except Exception as e:
                print(f"   ⚠️ Could not read EXIF: {e}")

            image.close()

        except Exception as e:
            print(f"   ⚠️ Error fixing rotation: {e}")

    def extract_face_encoding(self, image_path, model_name=None, expected_dimensions=None):
        """
        Wyciągnij encoding twarzy ze zdjęcia
        Zwraca: encoding (lista liczb) lub None
        
        Args:
            image_path: ścieżka do zdjęcia
            model_name: nazwa modelu (np. 'Facenet')
            expected_dimensions: oczekiwana liczba wymiarów
        """
        if model_name is None:
            model_name = DEEPFACE_PRIMARY_MODEL
        if expected_dimensions is None:
            expected_dimensions = PRIMARY_DIMENSIONS

        try:
            print(f"📸 Extracting encoding from: {image_path}")

            # Normalizuj ścieżkę
            full_path = self._normalize_path(image_path)

            print(f"📁 Using path: {full_path}")
            print(f"✅ File exists: {os.path.isfile(full_path)}")

            if not os.path.isfile(full_path):
                print(f"❌ File not found: {full_path}")
                return None

            # ⭐ NAPRAW ROTACJĘ
            self.fix_image_rotation(full_path)

            # Odczytaj obraz
            image = cv2.imread(full_path)
            if image is None:
                print(f"❌ Cannot read image: {full_path}")
                return None

            print(f"✅ Image loaded successfully")

            # Wyciągnij embedding za pomocą DeepFace
            print(f"🧠 Running DeepFace with model: {model_name}...")

            embedding = DeepFace.represent(
                img_path=full_path,
                model_name=model_name,
                enforce_detection=DETECTOR_ENFORCE,
                detector_backend=DETECTOR_BACKEND
            )

            if embedding and len(embedding) > 0:
                encoding = embedding[0]['embedding']
                print(f"✅ Encoding extracted successfully ({len(encoding)} dimensions)")
                
                # ⭐ WALIDACJA WYMIARÓW
                if len(encoding) != expected_dimensions:
                    print(f"⚠️ WARNING: Expected {expected_dimensions} dims, got {len(encoding)}")
                    print(f"   Model {model_name} produces {len(encoding)}-dim encodings!")
                
                return encoding
            else:
                print(f"❌ No face detected in image")
                # ⭐ FALLBACK: Spróbuj z enforce_detection=False
                print(f"🔄 Retrying with enforce_detection=False...")
                try:
                    embedding = DeepFace.represent(
                        img_path=full_path,
                        model_name=model_name,
                        enforce_detection=False,  # ← Klucz!
                        detector_backend=DETECTOR_BACKEND
                    )
                    if embedding and len(embedding) > 0:
                        encoding = embedding[0]['embedding']
                        print(f"✅ Encoding extracted successfully with fallback ({len(encoding)} dimensions)")
                        return encoding
                except Exception as fallback_e:
                    print(f"❌ Fallback also failed: {str(fallback_e)}")
                
                return None

        except Exception as e:
            print(f"❌ Error extracting encoding with {model_name}: {str(e)}")
            # ⭐ FALLBACK NA EXCEPTION
            print(f"🔄 Retrying with enforce_detection=False...")
            try:
                embedding = DeepFace.represent(
                    img_path=full_path,
                    model_name=model_name,
                    enforce_detection=False,
                    detector_backend=DETECTOR_BACKEND
                )
                if embedding and len(embedding) > 0:
                    encoding = embedding[0]['embedding']
                    print(f"✅ Encoding extracted successfully with fallback ({len(encoding)} dimensions)")
                    return encoding
            except Exception as fallback_e:
                print(f"❌ Fallback also failed: {str(fallback_e)}")
            
            return None

    def register_person(self, pesel, photo_path):
        """
        Zarejestruj osobę - wyciągnij i zapisz encoding + cechy
        """
        try:
            print(f"\n📝 Registering person: PESEL={pesel}")

            person = get_person_by_pesel(pesel)
            if not person:
                print(f"❌ Person not found in database: {pesel}")
                return False

            full_path = get_full_path(photo_path)
            print(f"📁 Photo path: {full_path}")

            # ⭐ WYCIĄGNIJ ENCODING Z GŁÓWNEGO MODELU (z walidacją wymiarów)
            encoding = self.extract_face_encoding(
                full_path,
                DEEPFACE_PRIMARY_MODEL,
                PRIMARY_DIMENSIONS
            )
            if encoding is None:
                print(f"❌ Failed to extract encoding for {pesel}")
                return False

            # ⭐ WYCIĄGNIJ CECHY SZCZEGÓLNE
            features = None
            if FEATURE_EXTRACTION_ENABLED:
                features = self.feature_analyzer.analyze_face_features(full_path)
                if features:
                    save_face_features(pesel, features)
                    print(f"✅ Features saved")

            # Zapisz encoding w bazie
            success = save_face_encoding(pesel, encoding, DEEPFACE_PRIMARY_MODEL)

            if success:
                print(f"✅ Person registered successfully: {person['first_name']} {person['last_name']}")
                if features:
                    print(f"   👁️  Eye color: {features.get('eye_color', {}).get('name', 'unknown')}")
                    print(f"   💇 Hair color: {features.get('hair_color', {}).get('name', 'unknown')}")
                    print(f"   👃 Nose width: {features.get('nose_width', {}).get('width_estimate', 'unknown')}")
                return True
            return False

        except Exception as e:
            print(f"❌ Error registering person: {str(e)}")
            return False

    def recognize_face(self, image_path):
        """
        ⭐ NAPRAWIONA WERSJA - ZWRACA PIERWSZY MATCH ZARAZ
        
        Rozpoznaj twarz ze zdjęcia - wielomodelowe porównanie + analiza cech
        Zwraca: {Rozpoznano: bool, pesel, name, confidence, features, ...}
        
        Strategia:
        1. Model 1 (Primary) - najsurowszy, zwraca na pierwszy match
        2. Jeśli Model 1 fail → Model 2 (Secondary)
        3. Jeśli Model 2 fail → Model 3 (Tertiary)
        4. Jeśli wszystkie fail → "Nie rozpoznano"
        """
        try:
            # Normalizuj ścieżkę
            full_path = self._normalize_path(image_path)

            print(f"\n{'=' * 70}")
            print(f"🔍 RECOGNIZE FACE ENDPOINT")
            print(f"{'=' * 70}")
            print(f"Photo Path: {image_path}")
            print(f"📁 Full path: {full_path}")
            print(f"✅ File exists: {os.path.isfile(full_path)}")

            if not os.path.isfile(full_path):
                print(f"❌ File not found: {full_path}")
                return {
                    "Rozpoznano": False,
                    "Wiadomosc": "Plik nie znaleziony"
                }

            # ⭐ NAPRAW ROTACJĘ
            self.fix_image_rotation(full_path)

            # ⭐ WYCIĄGNIJ CECHY Z NIEZNANEGO ZDJĘCIA
            query_features = None
            if FEATURE_EXTRACTION_ENABLED:
                print(f"👁️  Analyzing facial features...")
                query_features = self.feature_analyzer.analyze_face_features(full_path)

            # ═══════════════════════════════════════════════════════════════════════
            # ⭐ KLUCZOWA ZMIANA: Próbuj modele po kolei
            # ZWRACA ZARAZ NA PIERWSZY MATCH!
            # ═══════════════════════════════════════════════════════════════════════

            for model_config in self.models:
                model_name = model_config['name']
                threshold = model_config['threshold']
                expected_dims = model_config['dimensions']

                print(f"\n🔄 Trying model: {model_name} (threshold: {threshold}, dims: {expected_dims})")

                # Wyciągnij encoding
                query_encoding = self.extract_face_encoding(full_path, model_name, expected_dims)
                if query_encoding is None:
                    print(f"   ⚠️ Could not extract encoding with {model_name}")
                    continue  # Spróbuj następny model

                # Pobierz wszystkie encodingi z bazy
                stored_encodings = get_all_face_encodings()

                if not stored_encodings:
                    print("   ⚠️ No faces registered in database")
                    continue

                print(f"   📊 Loaded {len(stored_encodings)} encodings from face_encodings table")
                print(f"   🔎 Comparing with {len(stored_encodings)} stored faces...")

                best_match = None
                best_distance = float('inf')

                # Porównaj z każdym encodingiem w bazie
                for pesel, stored_encoding in stored_encodings.items():
                    try:
                        distance = self.calculate_distance(
                            np.array(query_encoding),
                            np.array(stored_encoding)
                        )

                        print(f"      📊 {pesel}: distance = {distance:.4f}")

                        if distance < best_distance:
                            best_distance = distance
                            best_match = pesel

                    except ValueError as e:
                        print(f"      ⚠️ {pesel}: Dimension mismatch - skipping")
                        continue

                # ⭐ KLUCZOWA ZMIANA: ZWRÓĆ NA PIERWSZY MATCH!
                if best_match and best_distance < threshold:
                    print(f"\n   ✅ MATCH FOUND with {model_name}!")
                    print(f"      PESEL: {best_match}")
                    print(f"      Distance: {best_distance:.4f}")

                    person = get_person_by_pesel(best_match)

                    # Konwertuj dystans na pewność
                    confidence = 1 - (best_distance / threshold)
                    confidence = max(0, min(1, confidence))

                    # ⭐ ANALIZA CECH SZCZEGÓLNYCH
                    feature_score = 1.0
                    feature_details = {}

                    if FEATURE_EXTRACTION_ENABLED and query_features:
                        stored_features = get_face_features(best_match)
                        if stored_features:
                            feature_score = self._compare_features(
                                query_features,
                                stored_features
                            )
                            feature_details = {
                                'eye_color_match': query_features.get('eye_color', {}).get('name') == stored_features.get('eye_color', {}).get('name'),
                                'hair_color_match': query_features.get('hair_color', {}).get('name') == stored_features.get('hair_color', {}).get('name'),
                                'feature_similarity': feature_score
                            }
                            print(f"      👁️  Feature similarity: {feature_score:.2%}")

                    # Połączony wynik
                    combined_score = (confidence * 0.7) + (feature_score * 0.3)

                    result = {
                        "Rozpoznano": True,
                        "Pesel": person['pesel'],
                        "Imie": person['first_name'],
                        "Nazwisko": person['last_name'],
                        "DataUrodzenia": person['date_of_birth'],
                        "Plec": person['gender'],
                        "Pewnosc": confidence,
                        "CechyWynik": feature_score,
                        "WynikPolaczony": combined_score,
                        "Model": model_name,
                        "Dystans": best_distance,
                        "SzczegolyCech": feature_details,
                        "Wiadomosc": f"Rozpoznano: {person['first_name']} {person['last_name']}"
                    }

                    # ⭐ NAJWAŻNIEJSZE: Zwróć ZARAZ!
                    print(f"\n✅ Returning result from model {model_name}")
                    print(f"{'=' * 70}\n")
                    return result

                else:
                    print(f"   ❌ NO MATCH with {model_name}")
                    if best_match:
                        print(f"      Best distance: {best_distance:.4f} (threshold: {threshold})")
                    else:
                        print(f"      No matches found in database")

            # ⭐ Jeśli ŻADEN model nie znalazł matcha
            print(f"\n{'=' * 70}")
            print(f"❌ NO MATCH WITH ANY MODEL")
            print(f"{'=' * 70}\n")
            
            return {
                "Rozpoznano": False,
                "Pesel": None,
                "Imie": None,
                "Nazwisko": None,
                "Pewnosc": 0,
                "CechyWynik": 0,
                "Wiadomosc": "Twarz nie została rozpoznana - brak dopasowania z żadnym modelem"
            }

        except Exception as e:
            print(f"❌ Error recognizing face: {str(e)}")
            import traceback
            traceback.print_exc()
            return {
                "Rozpoznano": False,
                "Wiadomosc": f"Błąd: {str(e)}"
            }

    def _compare_features(self, features1: dict, features2: dict) -> float:
        """
        ⭐ Porównaj cechy szczególne dwóch twarzy
        Zwraca wynik podobieństwa (0-1)
        """
        total_weight = 0
        weighted_score = 0

        # Porównaj każdą cechę
        for feature_name, weight in FEATURE_WEIGHTS.items():
            f1 = features1.get(feature_name)
            f2 = features2.get(feature_name)

            if f1 and f2:
                similarity = self._compare_single_feature(feature_name, f1, f2)
                weighted_score += similarity * weight
                total_weight += weight

        return weighted_score / total_weight if total_weight > 0 else 0

    @staticmethod
    def _compare_single_feature(feature_name: str, f1: dict, f2: dict) -> float:
        """
        Porównaj pojedynczą cechę
        Zwraca podobieństwo (0-1)
        """
        if feature_name in ['eye_color', 'hair_color']:
            # Porównaj nazwy kolorów
            if f1.get('name') == f2.get('name'):
                return 1.0
            # Sprawdź podobieństwo RGB
            if 'rgb' in f1 and 'rgb' in f2:
                diff = np.sqrt(sum((a - b) ** 2 for a, b in zip(f1['rgb'], f2['rgb'])))
                return max(0, 1 - (diff / 255))
            return 0.5

        elif feature_name == 'eye_distance':
            # Porównaj dystans między oczami
            d1 = f1.get('normalized_distance')
            d2 = f2.get('normalized_distance')
            if d1 and d2:
                diff = abs(d1 - d2)
                return max(0, 1 - (diff * 5))  # 5% różnicy = 0.95
            return 0.5

        elif feature_name in ['nose_width', 'mouth_width']:
            # Porównaj wymiary
            w1 = f1.get('width_pixels') or f1.get('width_estimate')
            w2 = f2.get('width_pixels') or f2.get('width_estimate')
            if w1 and w2:
                diff = abs(w1 - w2) / max(w1, w2)
                return max(0, 1 - diff)
            return 0.5

        elif feature_name == 'eyebrow_shape':
            # Porównaj kąt brwi
            a1 = f1.get('average_angle')
            a2 = f2.get('average_angle')
            if a1 is not None and a2 is not None:
                diff = abs(a1 - a2)
                return max(0, 1 - (diff / 45))  # 45° = 0
            return 0.5

        elif feature_name == 'facial_asymmetry':
            # Porównaj asymetrię
            s1 = f1.get('asymmetry_score', 0.5)
            s2 = f2.get('asymmetry_score', 0.5)
            diff = abs(s1 - s2)
            return max(0, 1 - diff)

        elif feature_name == 'skin_tone':
            # Porównaj ton skóry
            t1 = f1.get('skin_tone', 'unknown')
            t2 = f2.get('skin_tone', 'unknown')
            if t1 == t2:
                return 1.0
            return 0.6

        return 0.5

    @staticmethod
    def _normalize_path(image_path):
        """Normalizuj ścieżkę do pliku"""
        if image_path.startswith('/uploads/'):
            return os.path.abspath(
                os.path.join(
                    os.path.dirname(__file__),
                    '../uploads/',
                    image_path.replace('/uploads/', '')
                )
            )
        elif image_path.startswith('/'):
            return image_path
        else:
            return os.path.abspath(
                os.path.join(os.path.dirname(__file__), '..', image_path)
            )

    @staticmethod
    def calculate_distance(encoding1, encoding2):
        """
        Oblicz dystans Euklidesowy między dwoma encodingami
        
        ⭐ OBSŁUGUJE RÓŻNE WYMIARY - przycina do mniejszego wymiaru
        Wartości bliskie 0 = bardzo podobne
        """
        if len(encoding1) != len(encoding2):
            print(f"⚠️ WARNING: Dimension mismatch - {len(encoding1)} vs {len(encoding2)}")
            # Przycina do mniejszego wymiaru
            min_dim = min(len(encoding1), len(encoding2))
            encoding1 = encoding1[:min_dim]
            encoding2 = encoding2[:min_dim]
            print(f"✂️ Truncated to {min_dim} dimensions")
        
        return np.linalg.norm(encoding1 - encoding2)


def main():
    """Główna funkcja do testowania"""
    print("\n🎭 Advanced Face Recognition System - Test Mode\n")

    # Inicjalizuj rozpoznawacz
    recognizer = FaceRecognizer()

    # Pobierz argumenty z linii poleceń
    if len(sys.argv) > 1:
        command = sys.argv[1]

        if command == "register" and len(sys.argv) > 3:
            pesel = sys.argv[2]
            photo_path = sys.argv[3]
            result = recognizer.register_person(pesel, photo_path)
            print(json.dumps({"success": result}))

        elif command == "recognize" and len(sys.argv) > 2:
            image_path = sys.argv[2]
            result = recognizer.recognize_face(image_path)
            print(json.dumps(result, default=str))

        else:
            print("Usage:")
            print("  python face_recognition.py register <pesel> <photo_path>")
            print("  python face_recognition.py recognize <image_path>")
    else:
        print("Usage:")
        print("  python face_recognition.py register <pesel> <photo_path>")
        print("  python face_recognition.py recognize <image_path>")


if __name__ == "__main__":
    main()