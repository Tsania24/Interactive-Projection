import cv2
import mediapipe as mp
import socket
import math

# --- KONFIGURASI ---
UDP_IP = "127.0.0.1"
PORT_DATA = 5052
PORT_VIDEO = 5053

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

mp_hands = mp.solutions.hands
hands = mp_hands.Hands(max_num_hands=1, min_detection_confidence=0.7)
mp_draw = mp.solutions.drawing_utils

cap = cv2.VideoCapture(1)
# Resolusi rendah untuk performa & pengiriman video lancar
cap.set(3, 320)
cap.set(4, 240)

print("PYTHON CONTROLLER SIAP...")
print("Buka tangan = Gerak Kursor")
print("Kepal tangan = KLIK")

while True:
    success, img = cap.read()
    if not success: break

    img = cv2.flip(img, 1)
    h, w, c = img.shape
    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    results = hands.process(img_rgb)
    
    # Data Default
    zone_msg = "DIAM"
    pos_x = 0.5
    pos_y = 0.5
    is_click = 0

    if results.multi_hand_landmarks:
        for hand_lms in results.multi_hand_landmarks:
            mp_draw.draw_landmarks(img, hand_lms, mp_hands.HAND_CONNECTIONS)
            
            # 1. AMBIL POSISI (Pakai Jari Telunjuk biar akurat buat nunjuk)
            # Landmark 8 = Ujung Telunjuk
            index_tip = hand_lms.landmark[8]
            wrist = hand_lms.landmark[0]
            
            pos_x = index_tip.x
            pos_y = index_tip.y # Ingat: di MediaPipe Y terbalik (0 di atas)

            # 2. LOGIKA ZONA (Untuk Game Katak)
            wrist_x = wrist.x
            if wrist_x < 0.35: zone_msg = "KIRI"
            elif wrist_x > 0.65: zone_msg = "KANAN"
            else: zone_msg = "TENGAH"

            # 3. DETEKSI KEPALAN TANGAN (FIST) UNTUK KLIK
            # Logika: Cek apakah ujung jari ada di bawah pangkal jari
            # Landmark: 8(Telunjuk), 12(Tengah), 16(Manis), 20(Kelingking)
            # Bandingkan dengan PIP (Sendi tengah): 6, 10, 14, 18
            
            tips = [8, 12, 16, 20]
            pips = [6, 10, 14, 18]
            fingers_folded = 0
            
            for i in range(4):
                # Jika Y Tip > Y Pip, berarti jari nekuk ke bawah (karena Y makin besar ke bawah)
                if hand_lms.landmark[tips[i]].y > hand_lms.landmark[pips[i]].y:
                    fingers_folded += 1
            
            # Jika 4 jari terlipat = KLIK
            if fingers_folded >= 3: # 3 jari cukup biar gampang
                is_click = 1
                cv2.circle(img, (int(pos_x*w), int(pos_y*h)), 15, (0, 255, 255), cv2.FILLED)
                cv2.putText(img, "CLICK!", (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)

    # --- KIRIM DATA GABUNGAN ---
    # Format: "ZONA,X,Y,KLIK" (Contoh: "KIRI,0.2,0.5,1")
    final_msg = f"{zone_msg},{pos_x:.2f},{pos_y:.2f},{is_click}"
    
    sock.sendto(final_msg.encode(), (UDP_IP, PORT_DATA))

    # --- KIRIM VIDEO ---
    _, buffer = cv2.imencode('.jpg', img, [int(cv2.IMWRITE_JPEG_QUALITY), 50])
    try:
        sock.sendto(buffer.tobytes(), (UDP_IP, PORT_VIDEO))
    except: pass

    if cv2.waitKey(1) & 0xFF == ord('q'): break

cap.release()
cv2.destroyAllWindows()