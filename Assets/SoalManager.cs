using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class DataSoal
{
    [TextArea] public string pertanyaan;
    public string jawabanBenar;
    public string jawabanSalah1;
    public string jawabanSalah2;
}

[System.Serializable]
public class LevelLagu
{
    public string namaLagu;
    public AudioClip klipAudio; 
    public Sprite gambarHadiah; 
    public List<DataSoal> daftarSoal; 
}

public class SoalManager : MonoBehaviour
{
    [Header("Referensi Objek")]
    public GameObject lilyPadPrefab;
    public FrogController katak;
    public TMP_Text soalText;
    
    // --- AUDIO SYSTEM (DIPERBAIKI) ---
    [Header("Audio System")]
    public AudioSource musicPlayer; // Speaker BGM
    public AudioClip bgmMenu;       // Musik saat di Menu
    public AudioClip bgmGameOver;   // Musik saat Kalah

    [Header("UI Panels")]
    public GameObject panelMenu;    
    public GameObject panelWin;     
    public GameObject panelLose;    
    
    [Header("UI Elements")]
    public Image winImage;          
    public TMP_Text winTitle;
    public TMP_Text finalScoreText;
    
    [Header("UI Extra")]
    public GameObject cursorObject; 

    [Header("Setting Visual")]
    public int totalBarisDiLayar = 6;
    public float jarakBarisY = 3.5f;
    public float jarakKolomX = 5f;
    public float startY = -3.5f;

    [Header("Setting Ukuran")]
    public float skalaKatak = 0.3f;
    public float skalaLilyPad = 0.3f;

    public List<LevelLagu> databaseLagu; 

    private List<GameObject> activeRows = new List<GameObject>();
    private LevelLagu currentLevel;
    private int currentSoalIndex = 0;
    private int skor = 0;
    private bool isProcessing = false;
    private bool hasAnswered = false;
    private bool gameIsActive = false; 

    void Start()
    {
        if (cursorObject != null) cursorObject.SetActive(true);

        // Setup Animasi Awal
        SetupPanelAwal(panelMenu, true);
        SetupPanelAwal(panelWin, false);
        SetupPanelAwal(panelLose, false);

        // --- PLAY MUSIK MENU ---
        PlayBGM(bgmMenu);

        if (katak == null) katak = FindObjectOfType<FrogController>();
        if (katak != null) katak.isControlActive = false; 
        
        if (lilyPadPrefab == null) Debug.LogError("FATAL: LilyPad Prefab Kosong!");
    }

    // --- HELPER ANIMASI UI ---
    void SetupPanelAwal(GameObject panel, bool isVisible)
    {
        if (panel == null) return;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        panel.SetActive(isVisible);
        cg.alpha = isVisible ? 1 : 0;
        cg.interactable = isVisible;
        cg.blocksRaycasts = isVisible;
        panel.transform.localScale = isVisible ? Vector3.one : Vector3.one * 0.5f;
    }

    IEnumerator AnimasiPanel(GameObject panel, bool show, float duration = 0.4f)
    {
        if (panel == null) yield break;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;
        float endAlpha = show ? 1f : 0f;
        Vector3 startScale = panel.transform.localScale;
        Vector3 endScale = show ? Vector3.one : Vector3.one * 0.8f;

        if (show) 
        {
            panel.SetActive(true);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        else
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        float timer = 0f;
        while(timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;
            t = t * t * (3f - 2f * t); // Smooth step

            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            panel.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        cg.alpha = endAlpha;
        panel.transform.localScale = endScale;
        if (!show) panel.SetActive(false);
    }

    // --- FUNGSI GAME ---

    public void PilihLagu(int indexLagu)
    {
        if (indexLagu < 0 || indexLagu >= databaseLagu.Count) return;

        currentLevel = databaseLagu[indexLagu];
        currentSoalIndex = 0;
        skor = 0; 
        
        StartCoroutine(MulaiGameSequence());
    }

    IEnumerator MulaiGameSequence()
    {
        // Animasi Menu Hilang
        StartCoroutine(AnimasiPanel(panelMenu, false));
        if(panelLose) panelLose.SetActive(false);
        if(panelWin) panelWin.SetActive(false);
        if (cursorObject != null) cursorObject.SetActive(false);

        isProcessing = false;
        hasAnswered = false;
        gameIsActive = false;

        foreach(GameObject row in activeRows) Destroy(row);
        activeRows.Clear();

        for (int i = 0; i < totalBarisDiLayar; i++) SpawnRow(i);
        SetupSoalBaru();
        
        if (katak != null)
        {
            katak.transform.position = new Vector3(0, startY, -1f);
            katak.transform.localScale = new Vector3(skalaKatak, skalaKatak, 1f); 
            katak.isControlActive = false;
        }

        if(soalText) soalText.text = "SIAP...";
        yield return new WaitForSeconds(1.0f);
        
        if(soalText) soalText.text = "MULAI!";
        yield return new WaitForSeconds(0.5f);

        // --- PLAY MUSIK GAMEPLAY (LAGU DAERAH) ---
        if (currentLevel != null)
        {
            DataSoal data = currentLevel.daftarSoal[0];
            if(soalText) soalText.text = "1. " + data.pertanyaan;
            PlayBGM(currentLevel.klipAudio);
        }

        SetRowSafety(0, false);
        SetRowSafety(1, true);
        
        if (katak != null) katak.isControlActive = true; 
        gameIsActive = true; 
    }

    void Update()
    {
        if (!gameIsActive || katak == null || isProcessing || !katak.isControlActive) return;

        string rawData = UDPReceiver.receivedData;
        string data = "DIAM";

        if (!string.IsNullOrEmpty(rawData))
        {
            string[] parts = rawData.Split(',');
            if (parts.Length > 0) data = parts[0].Trim(); 
        }
        
        if (activeRows.Count > 1)
        {
            GameObject rowJawaban = activeRows[1];
            if (rowJawaban.transform.childCount < 3) return;

            Transform targetPad = null;

            if (data == "KIRI" || Input.GetKeyDown(KeyCode.A)) 
                targetPad = rowJawaban.transform.GetChild(0);
            else if (data == "TENGAH" || Input.GetKeyDown(KeyCode.W)) 
                targetPad = rowJawaban.transform.GetChild(1);
            else if (data == "KANAN" || Input.GetKeyDown(KeyCode.D)) 
                targetPad = rowJawaban.transform.GetChild(2);

            if (targetPad != null)
            {
                isProcessing = true; 
                foreach (Transform daun in rowJawaban.transform)
                {
                    DaunJawaban script = daun.GetComponent<DaunJawaban>();
                    if (daun != targetPad)
                    {
                        script.isActive = false; 
                        if(daun.GetComponent<Collider2D>()) daun.GetComponent<Collider2D>().enabled = false;
                    }
                }
                katak.JumpTo(targetPad.position);
            }
        }
    }

    public void CheckAnswer(DaunJawaban daun)
    {
        if (hasAnswered) return;
        hasAnswered = true;

        if (daun.isCorrect) StartCoroutine(ProsesBenar());
        else StartCoroutine(ProsesSalah(daun.gameObject));
    }

    IEnumerator ProsesBenar()
    {
        skor += 20;
        if(finalScoreText) finalScoreText.text = "Skor Akhir: " + skor;
        currentSoalIndex++; 
        yield return new WaitForSeconds(0.5f);

        float timer = 0f; float duration = 0.8f;
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        foreach(GameObject row in activeRows) {
            startPositions.Add(row.transform.position);
            targetPositions.Add(row.transform.position - new Vector3(0, jarakBarisY, 0));
        }
        
        Vector3 katakStart = katak.transform.position;
        Vector3 katakTarget = new Vector3(katakStart.x, startY, -1f);

        while (timer < duration) {
            timer += Time.deltaTime;
            float t = timer / duration;
            for (int i = 0; i < activeRows.Count; i++) activeRows[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            katak.transform.position = Vector3.Lerp(katakStart, katakTarget, t);
            yield return null;
        }

        GameObject oldRow = activeRows[0];
        activeRows.RemoveAt(0);
        Destroy(oldRow);

        SpawnRow(0); 
        GameObject newRow = activeRows[activeRows.Count - 1];
        float topY = activeRows[activeRows.Count - 2].transform.position.y + jarakBarisY;
        newRow.transform.position = new Vector3(0, topY, 0);

        if (currentSoalIndex >= currentLevel.daftarSoal.Count)
        {
            MenangGame();
        }
        else
        {
            SetupSoalBaru();
            SetRowSafety(0, false);
            SetRowSafety(1, true);
            
            katak.transform.position = new Vector3(katak.transform.position.x, startY, -1f);
            katak.isControlActive = true;
            isProcessing = false;
            hasAnswered = false;
        }
    }

    IEnumerator ProsesSalah(GameObject daunSalah)
    {
        float timer = 0f;
        Vector3 startScale = daunSalah.transform.localScale;
        Vector3 katakStartScale = katak.transform.localScale;

        while(timer < 0.8f) {
            timer += Time.deltaTime;
            float t = timer / 0.8f;
            daunSalah.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            katak.transform.localScale = Vector3.Lerp(katakStartScale, Vector3.zero, t);
            yield return null;
        }
        Destroy(daunSalah); 
        KalahGame(); 
    }

    void MenangGame()
    {
        gameIsActive = false;
        katak.isControlActive = false;
        
        if (cursorObject != null) cursorObject.SetActive(true);

        if(panelWin != null)
        {
            if(winTitle) winTitle.text = currentLevel.namaLagu;
            if(winImage) winImage.sprite = currentLevel.gambarHadiah;
            StartCoroutine(AnimasiPanel(panelWin, true));
        }
        
        // Tetap mainkan lagu daerah (biarkan looping)
    }

    void KalahGame()
    {
        gameIsActive = false;
        katak.isControlActive = false;
        
        if (cursorObject != null) cursorObject.SetActive(true);

        if (panelLose != null) 
        {
            if(finalScoreText) finalScoreText.text = "Skor Akhir: " + skor;
            StartCoroutine(AnimasiPanel(panelLose, true));
        }

        // --- PLAY MUSIK GAME OVER ---
        PlayBGM(bgmGameOver);
    }

    public void TombolMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void TombolUlangi()
    {
        skor = 0;
        currentSoalIndex = 0;
        StartCoroutine(AnimasiPanel(panelLose, false, 0.2f));
        StartCoroutine(MulaiGameSequence());
    }

    // --- AUDIO HELPER ---
    void PlayBGM(AudioClip clip)
    {
        if (musicPlayer == null || clip == null) return;
        if (musicPlayer.clip == clip && musicPlayer.isPlaying) return; // Jangan restart lagu yg sama

        musicPlayer.clip = clip;
        musicPlayer.loop = true;
        musicPlayer.Play();
    }

    // --- HELPER LAINNYA ---
    void SpawnRow(int rowNumber)
    {
        GameObject rowObj = new GameObject("Row_" + rowNumber);
        float posY = startY + (rowNumber * jarakBarisY);
        rowObj.transform.position = new Vector3(0, posY, 0);

        float[] xPos = { -jarakKolomX, 0, jarakKolomX };
        for (int i = 0; i < 3; i++)
        {
            if(lilyPadPrefab == null) return;

            GameObject daun = Instantiate(lilyPadPrefab, rowObj.transform);
            daun.transform.localPosition = new Vector3(xPos[i], 0, 0);
            daun.transform.localScale = new Vector3(skalaLilyPad, skalaLilyPad, 1f);

            if(!daun.GetComponent<DaunJawaban>()) daun.AddComponent<DaunJawaban>();
            TMP_Text txt = daun.GetComponentInChildren<TMP_Text>();
            if(txt) txt.text = "";
        }
        activeRows.Add(rowObj);
    }

    void SetupSoalBaru()
    {
        if (activeRows.Count < 2) return;
        GameObject row = activeRows[1];

        if (currentSoalIndex < currentLevel.daftarSoal.Count)
        {
            DataSoal data = currentLevel.daftarSoal[currentSoalIndex];
            if(soalText) soalText.text = (currentSoalIndex + 1) + ". " + data.pertanyaan;
            
            int posBenar = Random.Range(0, 3);
            int salahCounter = 0;

            for (int i = 0; i < 3; i++)
            {
                Transform daun = row.transform.GetChild(i);
                TMP_Text txt = daun.GetComponentInChildren<TMP_Text>();
                DaunJawaban script = daun.GetComponent<DaunJawaban>();
                
                if(daun.GetComponent<Collider2D>()) daun.GetComponent<Collider2D>().enabled = true;
                script.ResetTrigger();
                daun.localScale = new Vector3(skalaLilyPad, skalaLilyPad, 1f); 

                if (i == posBenar) {
                    txt.text = data.jawabanBenar;
                    daun.tag = "JawabanBenar";
                    script.isCorrect = true;
                } else {
                    if (salahCounter == 0) txt.text = data.jawabanSalah1;
                    else txt.text = data.jawabanSalah2;
                    salahCounter++;
                    daun.tag = "JawabanSalah";
                    script.isCorrect = false;
                }
            }
        }
    }
    
    void SetRowSafety(int rowIndex, bool isActive) {
        if (rowIndex >= activeRows.Count) return;
        GameObject row = activeRows[rowIndex];
        DaunJawaban[] daunScripts = row.GetComponentsInChildren<DaunJawaban>();
        foreach (var d in daunScripts) d.isActive = isActive;
    }
}