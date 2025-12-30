using UnityEngine;
using System.Collections;

public class FrogController : MonoBehaviour
{
    public float jumpSpeed = 5f;
    public bool isControlActive = true;
    
    [Header("Audio SFX")]
    public AudioSource sfxSource; // Sumber suara (Speaker)
    public AudioClip jumpSound;   // File suara lompat

    private Vector3 originalScale; 

    void Start()
    {
        originalScale = transform.localScale; 
        
        // Setup otomatis jika lupa diisi
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
    }

    public void JumpTo(Vector3 targetPos)
    {
        StartCoroutine(MoveRoutine(targetPos));
    }

    IEnumerator MoveRoutine(Vector3 target)
    {
        isControlActive = false;
        
        // --- MAINKAN SUARA LOMPAT DISINI ---
        if (sfxSource != null && jumpSound != null)
        {
            sfxSource.PlayOneShot(jumpSound);
        }
        // -----------------------------------

        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 finalTarget = new Vector3(target.x, target.y, -1f); 

        while (timer < 0.4f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.4f;
            float height = Mathf.Sin(t * Mathf.PI) * 2f; 
            transform.position = Vector3.Lerp(startPos, finalTarget, t) + new Vector3(0, height, 0);
            yield return null;
        }
        transform.position = finalTarget;
    }

    public void Respawn(Vector3 resetPos)
    {
        transform.position = resetPos;
        transform.localScale = originalScale; 
        isControlActive = true;
    }
}