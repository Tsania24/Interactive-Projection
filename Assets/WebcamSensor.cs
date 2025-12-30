using UnityEngine;
using UnityEngine.UI;

public class WebcamSensor : MonoBehaviour
{
    public RawImage display; // Tempat menampilkan gambar webcam

    // UI Indikator (Kotak yang menyala kalau ada gerakan)
    public Image indicatorLeft;
    public Image indicatorRight;

    // Variable Global yang bisa dibaca script lain
    public static bool IsMovingLeft = false;
    public static bool IsMovingRight = false;

    private WebCamTexture _webcam;
    private Color32[] _prevColors;
    private Color32[] _currColors;

    [Range(0.1f, 1f)]
    public float sensitivity = 0.5f; // Atur sensitivitas di Inspector

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            // Pakai resolusi rendah (320x240) supaya game tetap RINGAN & CEPAT
            _webcam = new WebCamTexture(devices[0].name, 320, 240, 30);
            display.texture = _webcam;
            display.material.mainTexture = _webcam;
            _webcam.Play();

            // Siapkan memori untuk data pixel
            _prevColors = new Color32[_webcam.width * _webcam.height];
            _currColors = new Color32[_webcam.width * _webcam.height];
        }
        else
        {
            Debug.LogError("Webcam tidak ditemukan!");
        }
    }

    void Update()
    {
        if (_webcam == null || !_webcam.didUpdateThisFrame) return;

        _webcam.GetPixels32(_currColors);

        if (_prevColors.Length != _currColors.Length)
        {
            _prevColors = _webcam.GetPixels32();
            return;
        }

        long movementLeft = 0;
        long movementRight = 0;
        int width = _webcam.width;
        int center = width / 2;

        // --- ALGORITMA DETEKSI GERAKAN (PIXEL DIFFERENCE) ---
        // Kita lompat setiap 5 pixel (y+=5, x+=5) untuk performa tinggi
        for (int y = 0; y < _webcam.height; y += 5)
        {
            for (int x = 0; x < width; x += 5)
            {
                int i = y * width + x;

                // Hitung beda warna pixel sekarang vs sebelumnya
                int diff = Mathf.Abs(_currColors[i].r - _prevColors[i].r) +
                           Mathf.Abs(_currColors[i].g - _prevColors[i].g) +
                           Mathf.Abs(_currColors[i].b - _prevColors[i].b);

                if (diff > 50) // Jika warnanya berubah drastis
                {
                    if (x < center) movementLeft++; // Gerakan di Kiri
                    else movementRight++;           // Gerakan di Kanan
                }
            }
        }

        // Update frame sebelumnya
        System.Array.Copy(_currColors, _prevColors, _currColors.Length);

        // --- PENENTUAN STATUS ---
        float threshold = (width * _webcam.height) / (300f * sensitivity);

        IsMovingLeft = movementLeft > threshold;
        IsMovingRight = movementRight > threshold;

        // Visual Feedback (Ubah warna indikator)
        if (indicatorLeft) indicatorLeft.color = IsMovingLeft ? Color.green : Color.red;
        if (indicatorRight) indicatorRight.color = IsMovingRight ? Color.green : Color.red;
    }
}