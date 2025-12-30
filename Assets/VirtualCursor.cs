using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class VirtualCursor : MonoBehaviour
{
    [Header("Setting")]
    public RectTransform cursorImage; 
    public Canvas mainCanvas; 
    public float smoothing = 10f; 

    // --- TAMBAHAN PENTING ---
    private Camera uiCamera; 

    private Vector2 targetPos;
    private bool isClicking = false;
    private bool wasClicking = false; 

    private PointerEventData pointerData;
    private List<RaycastResult> raycastResults;

    void Start()
    {
        pointerData = new PointerEventData(EventSystem.current);
        raycastResults = new List<RaycastResult>();

        // Otomatis cari kamera yang dipakai oleh Canvas
        if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = mainCanvas.worldCamera;
        }
        else
        {
            uiCamera = null; // Kalau overlay, tidak butuh kamera
        }
    }

    void Update()
    {
        string rawData = UDPReceiver.receivedData;
        if (string.IsNullOrEmpty(rawData)) return;

        string[] parts = rawData.Split(',');
        
        if (parts.Length >= 4)
        {
            float x = float.Parse(parts[1]);
            float y = 1f - float.Parse(parts[2]); 
            
            int clickStatus = int.Parse(parts[3]);
            isClicking = (clickStatus == 1);

            MoveCursor(x, y);
            
            if (isClicking && !wasClicking)
            {
                TryClickUI();
            }
            wasClicking = isClicking;
        }
    }

    void MoveCursor(float x, float y)
    {
        if (mainCanvas == null || cursorImage == null) return;

        float screenX = x * Screen.width;
        float screenY = y * Screen.height;

        Vector3 finalPosition;

        // --- PERBAIKAN LOGIKA POSISI ---
        if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCamera != null)
        {
            // Konversi Pixel ke World Space (sesuai jarak Plane Distance Canvas)
            // Kita ambil jarak dari Plane Distance canvas
            float distance = mainCanvas.planeDistance;
            
            // PENTING: ScreenToWorldPoint butuh Z (jarak dari kamera)
            Vector3 screenPoint = new Vector3(screenX, screenY, distance);
            finalPosition = uiCamera.ScreenToWorldPoint(screenPoint);
        }
        else
        {
            // Logika lama (untuk Overlay)
            finalPosition = new Vector3(screenX, screenY, 0);
        }

        // Gerakkan dengan Smooth
        cursorImage.position = Vector3.Lerp(cursorImage.position, finalPosition, Time.deltaTime * smoothing);
        
        // Ubah warna
        Image img = cursorImage.GetComponent<Image>();
        if(img) img.color = isClicking ? Color.red : Color.white;
    }

    void TryClickUI()
    {
        // Ubah posisi pointer data ke posisi layar (pixel) untuk Raycast
        // Karena Raycast UI selalu butuh koordinat layar, bukan dunia
        if (uiCamera != null)
        {
            pointerData.position = uiCamera.WorldToScreenPoint(cursorImage.position);
        }
        else
        {
            pointerData.position = cursorImage.position;
        }

        EventSystem.current.RaycastAll(pointerData, raycastResults);

        if (raycastResults.Count > 0)
        {
            GameObject hitObject = raycastResults[0].gameObject;
            Button btn = hitObject.GetComponentInParent<Button>(); 
            if (btn != null && btn.interactable)
            {
                btn.onClick.Invoke(); 
            }
        }
        raycastResults.Clear();
    }
}