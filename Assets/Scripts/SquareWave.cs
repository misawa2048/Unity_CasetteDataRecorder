using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class SquareWave : MonoBehaviour
{
    readonly int m_samplerate = 44100;
    [SerializeField] float m_bps = 1200;
    [SerializeField] InputField m_url = null;
    [SerializeField] Image m_progressImage = null;
    [SerializeField] Image m_ledImage = null;
    [SerializeField] Color m_ledOnColor = Color.white;
    [SerializeField] Color m_ledOffColor = Color.black;
    [SerializeField] AudioClip m_stopClip = null;
    int m_pulseW;
    int m_pulseWCtr;
    AudioSource m_ac;
    float[] m_sqWaves;
    int m_wavePtr;
    int m_m_pulseSpd;
    int[] m_bitDataArr = { 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1 };
    int m_bitPtr;
    byte[] m_fileBytes;
    int m_filePosPtr;
    int m_fileProgress;
    bool m_isStarted;
    bool m_isFinished;
    AudioClip m_clip;

    void Start()
    {
        m_sqWaves = new float[m_samplerate * 2];
        for(int i=0;i< m_sqWaves.Length; ++i)
        {
            m_sqWaves[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * m_bps * (float)i / m_samplerate) * 2f, -1f, 1f);
        }
        m_pulseW = m_samplerate / (int)m_bps;
        m_pulseWCtr = 0;
        m_filePosPtr = 0;
        m_fileProgress = 0;
        m_wavePtr = 0;
        m_m_pulseSpd = 1;
        m_bitPtr = 0;
        m_isStarted = false;
        m_isFinished = false;
        m_ac = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (m_isStarted)
        {
            m_ledImage.color = (m_m_pulseSpd == 1) ? m_ledOffColor : m_ledOnColor;
            m_progressImage.fillAmount = (float)m_fileProgress / (float)m_fileBytes.Length;
            if (m_isFinished)
            {
                m_ac.Stop();
            }
        }
    }

    void OnAudioRead(float[] data)
    {
        if (m_isFinished)
            return;

        if (!m_isStarted)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = m_sqWaves[m_wavePtr];
                m_wavePtr = (m_wavePtr + 1) % m_sqWaves.Length;
            }
            return;
        }

        for (int i = 0; i < data.Length; ++i)
        {
            data[i] = m_sqWaves[m_wavePtr];
            m_wavePtr = (m_wavePtr + m_m_pulseSpd) % m_sqWaves.Length;
            m_pulseWCtr++;
            if(m_pulseWCtr>= m_pulseW)
            {
                m_pulseWCtr -= m_pulseW;
                m_m_pulseSpd = m_bitDataArr[m_bitPtr]+1;
                m_bitPtr++;
                if(m_bitPtr>= m_bitDataArr.Length)
                {
                    m_bitPtr -= m_bitDataArr.Length;
                    if (setFileDataOne(m_isStarted))
                    {
                        m_isFinished = true;
                        break;
                    }
                }
            }
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
    }

    bool setFileDataOne(bool _isStarted)
    {
        if (_isStarted)
        {
            if(m_fileBytes.Length<= m_fileProgress)
            {
                return true;
            }
            //Debug.Log(m_fileProgress + "/" + m_fileBytes.Length);
            byte dat = m_fileBytes[m_filePosPtr];
            m_bitDataArr[0] = 0;
            for (int i = 0; i < 8; ++i)
            {
                m_bitDataArr[1 + i] = ((dat >> i) & 1);
            }
            m_bitDataArr[9] = 1;
            m_bitDataArr[10] = 1;
            m_filePosPtr = (m_filePosPtr + 1) % m_fileBytes.Length;
            m_fileProgress++;
        }
        else
        {
            for (int i = 0; i < m_bitDataArr.Length; ++i)
            {
                m_bitDataArr[i] = 0;
            }
        }
        return (m_fileProgress > m_fileBytes.Length);
    }

    public void OnCassetePlay()
    {
        OnCasseteStop();
        m_clip = AudioClip.Create("MySinusoid", m_samplerate * 2, 1, m_samplerate, true, OnAudioRead, OnAudioSetPosition);
        //m_clip = m_ac.clip;
        //m_clip.SetData(m_sqWaves, 0);
        m_ac.clip = m_clip;
        StartCoroutine(GetDataCo());
    }

    public void OnCasseteStop()
    {
        m_ledImage.color = m_ledOffColor;
        m_progressImage.fillAmount = 0;
        m_isStarted = false;
        m_isFinished = false;
        m_fileProgress = 0;
        m_ac.Stop();
        m_ac.PlayOneShot(m_stopClip);
    }


    IEnumerator GetDataCo()
    {
        m_ac.Stop();
        m_ac.Play();

        if (!m_url.text.StartsWith("http"))
        {
            m_fileBytes = System.Text.Encoding.ASCII.GetBytes(m_url.text);
            m_isStarted = true;
            yield break;
        }

        UnityWebRequest www = new UnityWebRequest(m_url.text);
        www.method = UnityWebRequest.kHttpVerbGET;
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.Log(www.error);
            OnCasseteStop();
        }
        else
        {
            Debug.Log("2:"+ www.responseCode);
            if (www.responseCode == 200)
            {
                m_fileBytes = www.downloadHandler.data;
                Debug.Log("!" + m_fileBytes.Length);
                m_isStarted = true;
            }
            else
            {
                OnCasseteStop();
            }
        }
        yield return null;
    }
}