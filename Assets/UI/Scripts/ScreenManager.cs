using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScreenManager : MonoBehaviour
{
    [Tooltip("Canvas / UIs principais")]
    [SerializeField] private Canvas[] canvas;

    [Tooltip("Toggle da tela cheia")]
    [SerializeField] private Toggle fullscreenToggle;

    [Tooltip("Menu em cascata da resolucao")]
    [SerializeField] private TMP_Dropdown resDropDown;

    [Tooltip("Largura resolucao")]
    [SerializeField] private int resWidth = 1920;
    [Tooltip("Altura resolucao")]
    [SerializeField] private int resHeight = 1080;

    // Matriz de todas resolucoes
    private Resolution[] resArray;
    // fullscreen ativado ou nao
    private bool isFullScreen = false;
    // resolucao selecionada
    private int dropdownIndex;


    void Start()
    {
        // inicializando player prefs

        if (!PlayerPrefs.HasKey("fullscreenOn"))
            PlayerPrefs.SetInt("fullscreenOn", (isFullScreen ? 1 : 0));
        else
            isFullScreen = (PlayerPrefs.GetInt("fullscreenOn") != 0);

        if (!PlayerPrefs.HasKey("resWidth"))
            PlayerPrefs.SetInt("resWidth", resWidth);
        else
            resWidth = PlayerPrefs.GetInt("resWidth");

        if (!PlayerPrefs.HasKey("resHeight"))
            PlayerPrefs.SetInt("resHeight", resHeight);
        else
            resHeight = PlayerPrefs.GetInt("resHeight");

        // inicializando array de resolucoes

        resArray = Screen.resolutions;

        List<string> resStringList = new List<string>();
        foreach (Resolution res in resArray)
        {
            resStringList.Add(res.ToString());
        }

        resDropDown.ClearOptions();
        resDropDown.AddOptions(resStringList);

        // escolhendo qual resolucao fica marcada no menu

        if (PlayerPrefs.HasKey("screenDDIndex"))
        {
            dropdownIndex = PlayerPrefs.GetInt("screenDDIndex");
        }
        else
        {
            // Primeira vez: marca a resolucao atual da tela,
            // e nao a primeira da lista (que e a menor de todas)
            dropdownIndex = resArray.Length - 1;
            for (int i = 0; i < resArray.Length; i++)
            {
                if (resArray[i].width == Screen.currentResolution.width &&
                    resArray[i].height == Screen.currentResolution.height)
                {
                    dropdownIndex = i;
                }
            }
            PlayerPrefs.SetInt("screenDDIndex", dropdownIndex);
        }

        // Garante que a posicao existe neste monitor
        if (dropdownIndex < 0 || dropdownIndex >= resArray.Length)
            dropdownIndex = resArray.Length - 1;

        // alterando o menu de opcoes basedo nos player prefs
        // (sem disparar SetRes / SetFullScreen so por abrir a tela)

        fullscreenToggle.SetIsOnWithoutNotify(isFullScreen);
        resDropDown.SetValueWithoutNotify(dropdownIndex);
    }

    public void SetRes()
    {
        // pega a resolucao do array na posicao escolhida
        dropdownIndex = resDropDown.value;

        resWidth = resArray[dropdownIndex].width;
        resHeight = resArray[dropdownIndex].height;

        // configura resolucao da tela
        Screen.SetResolution(resWidth, resHeight, isFullScreen);

        // salva prefs
        PlayerPrefs.SetInt("resWidth", resWidth);
        PlayerPrefs.SetInt("resHeight", resHeight);
        PlayerPrefs.SetInt("screenDDIndex", dropdownIndex);

        Debug.Log("[TELA] Nova resolucao: " + resWidth + " x " + resHeight);
        Debug.Log("[TELA] ScreenDDIndex guardado na pos " + dropdownIndex);
    }

    public void SetFullScreen()
    {
        // guarda valor do toggle
        isFullScreen = fullscreenToggle.isOn;

        // configura resolucao da tela
        Screen.SetResolution(resWidth, resHeight, isFullScreen);

        // salva prefs
        PlayerPrefs.SetInt("fullscreenOn", (isFullScreen ? 1 : 0));

        Debug.Log("[TELA] Fullscreen alterado para " + isFullScreen);
    }
}
