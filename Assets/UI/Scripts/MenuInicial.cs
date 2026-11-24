using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Coloque este script em um objeto vazio da cena Menu.
// Controla os botoes "Continuar" e "Novo Jogo" da tela inicial.
public class MenuInicial : MonoBehaviour
{
    [Tooltip("Botao Continuar (fica desativado se ainda nao existe progresso salvo)")]
    public Button botaoContinuar;

    [Tooltip("Cena que abre depois de clicar em Continuar ou Novo Jogo")]
    public string cenaFases = "MenuFasesNovo";

    private AudioManagerMenu audioManagerMenu;

    void Start()
    {
        audioManagerMenu = FindFirstObjectByType<AudioManagerMenu>();

        // So deixa clicar em Continuar se o jogador ja liberou alguma fase alem da 1
        int faseLiberada = PlayerPrefs.GetInt("faseDesbloqueada", 1);
        if (botaoContinuar != null)
        {
            botaoContinuar.interactable = faseLiberada > 1;
        }
    }

    // Botao Continuar: vai para a selecao de fases mantendo o progresso
    public void Continuar()
    {
        TocarSom();
        SceneManager.LoadScene(cenaFases);
    }

    // Botao Novo Jogo: tranca as fases 2 a 5 e vai para a selecao de fases
    public void NovoJogo()
    {
        TocarSom();
        PlayerPrefs.SetInt("faseDesbloqueada", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(cenaFases);
    }

    void TocarSom()
    {
        if (audioManagerMenu != null)
        {
            audioManagerMenu.PlaySelectSound();
        }
    }
}
