using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [Tooltip("Mixer de audio")]
    [SerializeField] private AudioMixer myMixer;
    [Tooltip("Objeto do slider de musica")]
    [SerializeField] private Slider musicVolSlider;
    [Tooltip("Objeto do slider de efeitos sonoros")]
    [SerializeField] private Slider soundVolSlider;

    void Start()
    {
        if (!PlayerPrefs.HasKey("musicVolume"))
            PlayerPrefs.SetFloat("musicVolume", 0f);
        if (!PlayerPrefs.HasKey("soundVolume"))
            PlayerPrefs.SetFloat("soundVolume", 0f);

        if (!myMixer)
            Debug.LogWarning("[AUDIO] Mixer faltando");
        if (!musicVolSlider || !soundVolSlider)
            Debug.LogWarning("[AUDIO] Sliders faltando");
        else
            LoadPrefs();
    }

    public void ChangeMusicVolume()
    {
        Debug.Log("[AUDIO] valor do volume de musica alterado para " + musicVolSlider.value);
        float valor = musicVolSlider.value;
        // No minimo do slider, silencia de verdade (-80 dB = mudo)
        if (valor <= musicVolSlider.minValue)
        {
            valor = -80f;
        }
        myMixer.SetFloat("MusicVol", valor);
        PlayerPrefs.SetFloat("musicVolume", musicVolSlider.value);
    }

    public void ChangeSoundVolume()
    {
        Debug.Log("[AUDIO] valor do volume de som alterado para " +  soundVolSlider.value);
        float valor = soundVolSlider.value;
        // No minimo do slider, silencia de verdade (-80 dB = mudo)
        if (valor <= soundVolSlider.minValue)
        {
            valor = -80f;
        }
        myMixer.SetFloat("SoundVol", valor);
        PlayerPrefs.SetFloat("soundVolume", soundVolSlider.value);
    }

    public void LoadPrefs()
    {
        musicVolSlider.value = PlayerPrefs.GetFloat("musicVolume");
        soundVolSlider.value = PlayerPrefs.GetFloat("soundVolume");
    }
}
