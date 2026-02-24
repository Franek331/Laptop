#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import cv2
import numpy as np
from typing import Dict, List, Tuple
import colorsys


class FaceFeatureAnalyzer:
    """
    ⭐ Analizator cech szczególnych twarzy
    Patrzy na: kolor oczu, włosy, brwi, nos, usta, pieprzyki, rozstawy
    """

    def __init__(self):
        """Inicjalizuj analizator cech"""
        self.feature_weights = {
            'eye_color': 0.20,
            'hair_color': 0.15,
            'eye_distance': 0.15,
            'nose_width': 0.12,
            'mouth_width': 0.10,
            'eyebrow_shape': 0.10,
            'facial_landmarks': 0.10,
            'skin_texture': 0.08
        }

    def analyze_face_features(self, image_path: str) -> Dict:
        """
        Analizuj cechy szczególne twarzy
        Zwraca: {eye_color, hair_color, eye_distance, nose_features, mouth_features, ...}
        """
        try:
            print(f"\n👁️  Analyzing facial features...")

            image = cv2.imread(image_path)
            if image is None:
                print(f"❌ Cannot read image: {image_path}")
                return None

            # Konwertuj BGR do RGB
            rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            hsv_image = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)

            features = {
                'eye_color': self._analyze_eye_color(rgb_image),
                'hair_color': self._analyze_hair_color(rgb_image),
                'eye_distance': self._estimate_eye_distance(image),
                'nose_width': self._estimate_nose_width(image),
                'mouth_width': self._estimate_mouth_width(image),
                'eyebrow_shape': self._analyze_eyebrow_shape(image),
                'skin_features': self._analyze_skin_texture(rgb_image),
                'facial_asymmetry': self._analyze_facial_asymmetry(image),
                'age_estimate': self._estimate_age(rgb_image),
                'skin_tone': self._analyze_skin_tone(rgb_image)
            }

            print(f"✅ Features analyzed:")
            for key, value in features.items():
                if value:
                    print(f"   📊 {key}: {value}")

            return features

        except Exception as e:
            print(f"❌ Error analyzing features: {str(e)}")
            return None

    def _analyze_eye_color(self, image) -> Dict:
        """Analizuj kolor oczu z górnej części twarzy"""
        try:
            # Weź górną część obrazu (gdzie zwykle są oczy)
            height = image.shape[0]
            width = image.shape[1]

            # ROI dla oczu - górna 1/3 obrazu, środek
            eye_roi = image[int(height * 0.15):int(height * 0.35),
                      int(width * 0.25):int(width * 0.75)]

            if eye_roi.size == 0:
                return None

            # Analiza dominujących kolorów w obszerze oczu
            dominant_color = self._get_dominant_color(eye_roi)

            return {
                'dominant_color': dominant_color,  # ⭐ Teraz lista zamiast ndarray
                'rgb': tuple(dominant_color),
                'name': self._color_name(dominant_color)
            }

        except Exception as e:
            print(f"   ⚠️  Error analyzing eye color: {e}")
            return None

    def _analyze_hair_color(self, image) -> Dict:
        """Analizuj kolor włosów z górnej części głowy"""
        try:
            # Górna część obrazu (włosy)
            height = image.shape[0]
            hair_roi = image[0:int(height * 0.25), :]

            if hair_roi.size == 0:
                return None

            dominant_color = self._get_dominant_color(hair_roi)

            return {
                'dominant_color': dominant_color,  # ⭐ Teraz lista zamiast ndarray
                'rgb': tuple(dominant_color),
                'name': self._color_name(dominant_color)
            }

        except Exception as e:
            print(f"   ⚠️  Error analyzing hair color: {e}")
            return None

    def _estimate_eye_distance(self, image) -> Dict:
        """Estymuj rozstaw między oczami"""
        try:
            # Szukaj źrenic w górnej części twarzy
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

            # Detektor twarzy Haara dla oczu
            eye_cascade = cv2.CascadeClassifier(
                cv2.data.haarcascades + 'haarcascade_eye.xml'
            )

            eyes = eye_cascade.detectMultiScale(gray, 1.3, 5)

            if len(eyes) >= 2:
                # Posortuj oczy od lewej do prawej
                eyes = sorted(eyes, key=lambda e: e[0])

                # Odległość między środkami oczu
                x1, y1, w1, h1 = eyes[0]
                x2, y2, w2, h2 = eyes[1]

                center1 = (x1 + w1 // 2, y1 + h1 // 2)
                center2 = (x2 + w2 // 2, y2 + h2 // 2)

                distance = np.sqrt((center2[0] - center1[0]) ** 2 + (center2[1] - center1[1]) ** 2)
                distance_norm = distance / image.shape[1]  # Normalizuj do szerokości

                return {
                    'pixel_distance': float(distance),
                    'normalized_distance': float(distance_norm),
                    'eyes_detected': 2
                }

            return {
                'pixel_distance': None,
                'normalized_distance': None,
                'eyes_detected': len(eyes)
            }

        except Exception as e:
            print(f"   ⚠️  Error estimating eye distance: {e}")
            return None

    def _estimate_nose_width(self, image) -> Dict:
        """Estymuj szerokość nosa"""
        try:
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
            height = gray.shape[0]
            width = gray.shape[1]

            # ROI dla nosa - środek twarzy
            nose_roi = gray[int(height * 0.4):int(height * 0.7),
                       int(width * 0.35):int(width * 0.65)]

            if nose_roi.size == 0:
                return None

            # Szukaj pionowych krawędzi (krawędzie nosa)
            sobelx = cv2.Sobel(nose_roi, cv2.CV_64F, 1, 0, ksize=3)
            sobelx = np.abs(sobelx)

            # Szerokość nosa - obszar o wysokich wartościach gradientu
            width_estimate = np.mean(sobelx > np.percentile(sobelx, 75))

            return {
                'width_estimate': float(width_estimate),
                'relative_width': float(sobelx.shape[1] / width)
            }

        except Exception as e:
            print(f"   ⚠️  Error estimating nose width: {e}")
            return None

    def _estimate_mouth_width(self, image) -> Dict:
        """Estymuj szerokość ust"""
        try:
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
            height = gray.shape[0]
            width = gray.shape[1]

            # ROI dla ust - dolna część twarzy
            mouth_roi = gray[int(height * 0.65):int(height * 0.85),
                        int(width * 0.3):int(width * 0.7)]

            if mouth_roi.size == 0:
                return None

            # Detektor ust (proste przybliżenie przez kontrast)
            edges = cv2.Canny(mouth_roi, 100, 200)
            contours, _ = cv2.findContours(edges, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

            if contours:
                # Największy kontur = usta
                largest_contour = max(contours, key=cv2.contourArea)
                x, y, w, h = cv2.boundingRect(largest_contour)

                return {
                    'width_pixels': int(w),
                    'height_pixels': int(h),
                    'aspect_ratio': float(w / h) if h > 0 else 0
                }

            return {
                'width_pixels': None,
                'height_pixels': None,
                'aspect_ratio': None
            }

        except Exception as e:
            print(f"   ⚠️  Error estimating mouth width: {e}")
            return None

    def _analyze_eyebrow_shape(self, image) -> Dict:
        """Analizuj kształt brwi"""
        try:
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
            height = gray.shape[0]
            width = gray.shape[1]

            # ROI dla brwi - górna część między oczami
            eyebrow_roi = gray[int(height * 0.15):int(height * 0.30),
                          int(width * 0.25):int(width * 0.75)]

            if eyebrow_roi.size == 0:
                return None

            # Detektuj krawędzie dla brwi
            edges = cv2.Canny(eyebrow_roi, 50, 150)

            # Oblicz median linii (czy są pochylone?)
            contours, _ = cv2.findContours(edges, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

            if contours:
                # Analizuj orientację konturów
                moments = [cv2.moments(cnt) for cnt in contours]
                angles = []

                for m in moments:
                    if m['m00'] != 0:
                        angle = 0.5 * np.arctan2(2 * m['m11'], m['m20'] - m['m02'])
                        angles.append(np.degrees(angle))

                if angles:
                    avg_angle = float(np.mean(angles))  # ⭐ Convert to Python float
                    return {
                        'average_angle': avg_angle,
                        'shape_description': self._describe_eyebrow_shape(avg_angle),
                        'contours_count': len(contours)
                    }

            return {
                'average_angle': None,
                'shape_description': 'unknown',
                'contours_count': 0
            }

        except Exception as e:
            print(f"   ⚠️  Error analyzing eyebrow shape: {e}")
            return None

    def _analyze_skin_texture(self, image) -> Dict:
        """Analizuj teksturę skóry - pieprzyki, znamiona, znaki"""
        try:
            # Konwertuj na RGB jeśli potrzeba
            if len(image.shape) == 2:
                image = cv2.cvtColor(image, cv2.COLOR_GRAY2RGB)

            # Analiza tonu skóry w różnych obszarach
            gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)

            # Detektor pieprzków/piegów - ciemne znaki na skórze
            # Używaj adaptacyjnego progu
            blurred = cv2.GaussianBlur(gray, (5, 5), 0)
            _, binary = cv2.threshold(blurred, 80, 255, cv2.THRESH_BINARY_INV)

            contours, _ = cv2.findContours(binary, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

            # Filtruj kontury po rozmiarze (pieprzki to małe znaki)
            freckles = [c for c in contours if 5 < cv2.contourArea(c) < 500]

            return {
                'freckles_detected': len(freckles),
                'texture_roughness': float(np.std(gray) / np.mean(gray)) if np.mean(gray) > 0 else 0,
                'blemishes_count': len(freckles),
                'texture_description': f"{len(freckles)} marks/freckles detected"
            }

        except Exception as e:
            print(f"   ⚠️  Error analyzing skin texture: {e}")
            return None

    def _analyze_facial_asymmetry(self, image) -> Dict:
        """Analizuj asymetrię twarzy"""
        try:
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
            height = gray.shape[0]
            width = gray.shape[1]

            # Podziel twarz na lewą i prawą połowę
            mid = width // 2

            left_half = gray[:, :mid]
            right_half = gray[:, mid:]

            # Odbij prawą połowę
            right_half_flipped = cv2.flip(right_half, 1)

            # Dopasuj rozmiary
            if left_half.shape[1] > right_half_flipped.shape[1]:
                left_half = left_half[:, :right_half_flipped.shape[1]]
            elif right_half_flipped.shape[1] > left_half.shape[1]:
                right_half_flipped = right_half_flipped[:, :left_half.shape[1]]

            # Oblicz różnicę (im większa, tym bardziej asymetryczna)
            diff = cv2.absdiff(left_half, right_half_flipped)
            asymmetry_score = float(np.mean(diff) / 255.0)  # ⭐ Convert to Python float

            return {
                'asymmetry_score': asymmetry_score,
                'symmetry_level': 1.0 - asymmetry_score,
                'description': 'symmetric' if asymmetry_score < 0.15 else 'slightly asymmetric' if asymmetry_score < 0.3 else 'asymmetric'
            }

        except Exception as e:
            print(f"   ⚠️  Error analyzing asymmetry: {e}")
            return None

    def _estimate_age(self, image) -> Dict:
        """Estymuj przybliżony wiek"""
        try:
            gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)

            # Zmarszczki = wyższe częstości w transformacie Fouriera
            f_transform = np.fft.fft2(gray)
            f_shift = np.fft.fftshift(f_transform)

            # Energia wysokich częstości ~ zmarszczki
            magnitude = np.abs(f_shift)
            high_freq_energy = float(np.mean(magnitude[magnitude > np.percentile(magnitude, 85)]))  # ⭐ Convert
            low_freq_energy = float(np.mean(magnitude[magnitude < np.percentile(magnitude, 15)]))  # ⭐ Convert

            wrinkle_score = float(high_freq_energy / (low_freq_energy + 1e-6))  # ⭐ Convert

            return {
                'wrinkle_score': wrinkle_score,
                'estimated_age_group': self._wrinkle_to_age_group(wrinkle_score),
                'confidence': 'low'  # Zawsze niskie, bo to przybliżenie
            }

        except Exception as e:
            print(f"   ⚠️  Error estimating age: {e}")
            return None

    def _analyze_skin_tone(self, image) -> Dict:
        """Analizuj ton skóry"""
        try:
            # Weź obszar twarzy (bez włosów i oczu)
            height = image.shape[0]
            width = image.shape[1]

            # ROI dla policzka
            cheek_roi = image[int(height * 0.35):int(height * 0.65),
                        int(width * 0.1):int(width * 0.4)]

            if cheek_roi.size == 0:
                return None

            dominant_color = self._get_dominant_color(cheek_roi)

            return {
                'skin_tone': self._color_name(dominant_color),
                'rgb': tuple(dominant_color),
                'hue': float(self._rgb_to_hue(dominant_color))  # ⭐ Convert to Python float
            }

        except Exception as e:
            print(f"   ⚠️  Error analyzing skin tone: {e}")
            return None

    @staticmethod
    def _get_dominant_color(image) -> list:
        """Znajdź dominujący kolor w obrazie - ZWRACA LISTĘ (JSON serializable)"""
        # Reshape do listy pikseli
        pixels = image.reshape((-1, 3))
        pixels = np.float32(pixels)

        # K-means clustering
        criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 10, 1.0)
        _, _, centers = cv2.kmeans(pixels, 3, None, criteria, 10, cv2.KMEANS_RANDOM_CENTERS)

        # Zwróć najczęstszy kolor
        centers = np.uint8(centers)
        # ⭐ KONWERTUJ NA LISTĘ (aby był JSON serializable)
        return centers[0].tolist()

    @staticmethod
    def _color_name(rgb) -> str:
        """Zwróć nazwę koloru z RGB"""
        # ⭐ Handle zarówno list jak i tuple
        if isinstance(rgb, (list, tuple)):
            r, g, b = rgb[0], rgb[1], rgb[2]
        else:
            r, g, b = rgb

        if r > 150 and g > 100 and b > 100:
            return 'light' if max(r, g, b) > 200 else 'medium'
        elif r > g and r > b:
            return 'brown/dark' if r < 150 else 'reddish'
        elif g > r and g > b:
            return 'greenish'
        elif b > r and b > g:
            return 'bluish'
        elif abs(r - g) < 30 and abs(g - b) < 30:
            return 'gray' if r < 128 else 'light_gray'
        else:
            return 'mixed'

    @staticmethod
    def _rgb_to_hue(rgb) -> float:
        """Konwertuj RGB na hue (0-360) - ZWRACA PYTHON FLOAT"""
        if isinstance(rgb, (list, tuple)):
            r, g, b = [x / 255.0 for x in rgb]
        else:
            r, g, b = [x / 255.0 for x in rgb]

        h, s, v = colorsys.rgb_to_hsv(r, g, b)
        return float(h * 360)  # ⭐ Explicit conversion

    @staticmethod
    def _describe_eyebrow_shape(angle: float) -> str:
        """Opisz kształt brwi na podstawie kąta"""
        angle = abs(angle)
        if angle < 10:
            return 'straight'
        elif angle < 20:
            return 'slightly_angled'
        elif angle < 35:
            return 'angled'
        else:
            return 'highly_angled'

    @staticmethod
    def _wrinkle_to_age_group(wrinkle_score: float) -> str:
        """Konwertuj wynik zmarszczek na grupę wiekową"""
        if wrinkle_score < 1.5:
            return 'young (18-25)'
        elif wrinkle_score < 2.0:
            return 'adult (25-35)'
        elif wrinkle_score < 2.5:
            return 'mature (35-50)'
        else:
            return 'older (50+)'


print("✅ FaceFeatureAnalyzer loaded")