using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

public class CameraReceiver : MonoBehaviour
{
    public RawImage rawImage;
    private Texture2D tex;
    private Thread receiveThread;
    private UdpClient client;
    private bool isRunning = false;

    void Start()
    {
        tex = new Texture2D(2, 2);
        rawImage.texture = tex;
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.Start();
    }

    void ReceiveData()
    {
        client = new UdpClient(5053); // Port untuk menerima gambar
        isRunning = true;
        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                if (data.Length > 8)
                {
                    int length = System.BitConverter.ToInt32(data, 0);
                    byte[] imageData = new byte[length];
                    System.Array.Copy(data, 4, imageData, 0, length);
                    tex.LoadImage(imageData);
                    tex.Apply();
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null)
        {
            receiveThread.Abort();
        }
        if (client != null)
        {
            client.Close();
        }
    }
}
