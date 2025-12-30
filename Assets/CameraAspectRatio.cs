using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    // Target rasio 4:3
    public float targetAspect = 4.0f / 3.0f;

    void Start()
    {
        // Hitung rasio layar saat ini (misal 1920/1080 = 1.77)
        float windowAspect = (float)Screen.width / (float)Screen.height;
        
        // Hitung seberapa perlu kita mengecilkan tampilan
        float scaleHeight = windowAspect / targetAspect;

        Camera camera = GetComponent<Camera>();

        // Jika layar terlalu lebar (16:9), kita tambahkan bar hitam di kiri-kanan (Pillarbox)
        if (scaleHeight < 1.0f)
        {
            Rect rect = camera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            camera.rect = rect;
        }
        else // Jika layar terlalu tinggi (jarang terjadi di laptop), tambah bar atas-bawah
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = camera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            camera.rect = rect;
        }
    }
}