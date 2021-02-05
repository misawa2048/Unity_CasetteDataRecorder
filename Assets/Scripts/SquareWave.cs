using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class SquareWave : MonoBehaviour
{
    [SerializeField] TextAsset m_textAsset = null;
    [SerializeField] string m_url = "http://www.gutenberg.org/cache/epub/20781/pg20781.txt";
    AudioSource m_ac;
    public int position = 0;
    public int samplerate = 44100;
    public float frequency = 440;
    float[] m_sqWaves;
    int wavePtr;
    int pulseW;
    int pulseWCtr;
    int pulseSpd;
    int[] bitDataArr = { 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1 };
    int bitPtr;
    byte[] m_fileBytes;
    int m_filePosPtr;
    bool m_isStarted;
    bool m_isFinished;
    AudioClip m_clip;

    void Start()
    {
        m_fileBytes = m_textAsset.bytes;
        m_filePosPtr = 0;
//        setFileDataOne(false);
        m_sqWaves = new float[samplerate * 2];
        for(int i=0;i< m_sqWaves.Length; ++i)
        {
            m_sqWaves[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * frequency * (float)i / samplerate) * 2f, -1f, 1f);
        }
        wavePtr = 0;
        pulseW = samplerate / 1200;
        pulseWCtr = 0;
        pulseSpd = 1;
        bitPtr = 0;
        m_isStarted = false;
        m_isFinished = false;
        m_clip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
        m_ac = GetComponent<AudioSource>();
        m_ac.clip = m_clip;
        m_ac.Play();
        StartCoroutine(GetDataCo());
    }

    private void Update()
    {
        if (m_ac.isPlaying)
        {
            if (m_isFinished)
            {
                m_ac.Stop();
            }
        }
    }

    void OnAudioRead(float[] data)
    {
        for(int i = 0; i < data.Length; ++i)
        {
            data[i] = m_sqWaves[wavePtr];
            wavePtr = (wavePtr + pulseSpd) % m_sqWaves.Length;
            pulseWCtr++;
            if(pulseWCtr>= pulseW)
            {
                pulseWCtr -= pulseW;
                pulseSpd = bitDataArr[bitPtr]+1;
                bitPtr++;
                if(bitPtr>= bitDataArr.Length)
                {
                    if (setFileDataOne(m_isStarted))
                    {
                        m_isFinished = true;
                    }
                    bitPtr -= bitDataArr.Length;
                }
            }
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }

    bool setFileDataOne(bool _isStarted)
    {
        if (_isStarted)
        {
            Debug.Log(m_filePosPtr + "/" + m_fileBytes.Length);
            byte dat = m_fileBytes[m_filePosPtr];
            bitDataArr[0] = 0;
            for (int i = 0; i < 8; ++i)
            {
                bitDataArr[1 + i] = ((dat >> i) & 1);
            }
            bitDataArr[9] = 1;
            bitDataArr[10] = 1;
            m_filePosPtr = (m_filePosPtr + 1) % m_fileBytes.Length;
            return (m_filePosPtr == 0);
        }
        else
        {
            for (int i = 0; i < bitDataArr.Length; ++i)
            {
                bitDataArr[i] = 0;
            }
            return false;
        }
    }

    IEnumerator GetDataCo()
    {
        Debug.Log("1");

        UnityWebRequest www = new UnityWebRequest(m_url);
        www.method = UnityWebRequest.kHttpVerbGET;
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("2:"+ www.responseCode);
            if (www.responseCode == 200)
            {
                // UTF8•¶Žš—ñ‚Æ‚µ‚ÄŽæ“¾‚·‚é
                string text = www.downloadHandler.text;

                Debug.Log("!"+text);
                m_fileBytes = www.downloadHandler.data;

                Debug.Log("3");
                m_isStarted = true;
                m_ac.Play();
            }
        }
        yield return null;
    }
}