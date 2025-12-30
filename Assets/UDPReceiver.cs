using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;

public class UDPReceiver : MonoBehaviour
{
    [Header("UI Kamera")]
    public RawImage layarKamera; 

    private UdpClient clientData;
    private UdpClient clientVideo;
    
    private Thread threadData;
    private Thread threadVideo;

    public int portData = 5052;
    public int portVideo = 5053;
    
    public bool isRunning = true;
    public static string receivedData = "DIAM";

    private byte[] imageBytes;
    private bool hasNewImage = false;
    private Texture2D tex;

    void Start()
    {
        isRunning = true; // Pastikan true saat mulai

        tex = new Texture2D(320, 240);
        if (layarKamera != null) layarKamera.texture = tex;

        threadData = new Thread(new ThreadStart(ReceiveData));
        threadData.IsBackground = true;
        threadData.Start();

        threadVideo = new Thread(new ThreadStart(ReceiveVideo));
        threadVideo.IsBackground = true;
        threadVideo.Start();
    }

    void Update()
    {
        if (hasNewImage && imageBytes != null)
        {
            try
            {
                tex.LoadImage(imageBytes);
                tex.Apply();
            }
            catch {}
            hasNewImage = false;
        }
    }

    private void ReceiveData()
    {
        try 
        {
            clientData = new UdpClient(portData);
            while (isRunning)
            {
                try
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = clientData.Receive(ref anyIP);
                    receivedData = Encoding.UTF8.GetString(data);
                }
                catch (System.Exception) 
                {
                    // Catch block kosong agar error koneksi ringan tidak spam console
                }
            }
        }
        catch (System.Exception)
        {
            // Error saat inisialisasi port (misal port terpakai)
        }
    }

    private void ReceiveVideo()
    {
        try
        {
            clientVideo = new UdpClient(portVideo);
            while (isRunning)
            {
                try
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = clientVideo.Receive(ref anyIP);
                    imageBytes = data;
                    hasNewImage = true;
                }
                catch (System.Exception) 
                {
                    // Catch block kosong -> Diam saja kalau ada error video
                }
            }
        }
        catch (System.Exception)
        {
             // Error inisialisasi video
        }
    }

    // --- FUNGSI PEMBERSIHAN ---
    void OnDestroy()
    {
        Cleanup();
    }

    void OnApplicationQuit()
    {
        Cleanup();
    }

    void Cleanup()
    {
        isRunning = false;
        
        // Tutup Client dulu
        if (clientData != null) clientData.Close();
        if (clientVideo != null) clientVideo.Close();
        
        // Baru matikan thread
        // Kita pakai Try-Catch disini agar thread yang mati tidak teriak error
        try {
            if (threadData != null && threadData.IsAlive) threadData.Abort();
        } catch {}

        try {
            if (threadVideo != null && threadVideo.IsAlive) threadVideo.Abort();
        } catch {}
    }
}