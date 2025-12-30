using UnityEngine;

public class DaunJawaban : MonoBehaviour
{
    public bool isCorrect = false;
    public bool isActive = false; // Pengaman agar tidak ditabrak saat jadi hiasan
    
    private bool hasTriggered = false;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Safety: Jangan aktif di 1 detik pertama spawn
        if (Time.time < spawnTime + 1.0f) return;

        // 2. Cek apakah daun ini sedang Aktif (Baris Soal)?
        if (!isActive) return;

        // 3. Mencegah trigger ganda
        if (hasTriggered) return;
        
        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            SoalManager manager = FindObjectOfType<SoalManager>();
            
            if (manager != null)
            {
                // LAPOR SAJA, JANGAN LAKUKAN APAPUN DISINI!
                // Biarkan SoalManager yang menentukan nasib daun ini.
                manager.CheckAnswer(this);
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        isActive = false;
    }
}