using UnityEngine;
using UnityEngine.UI;

// Coloque este script em um objeto vazio da cena MenuFasesNovo.
// Quando o menu abre, ele tranca os botões das fases que ainda não foram liberadas
// e mostra o cadeado em cima delas.
public class TrancaFases : MonoBehaviour
{
    // Arraste aqui os botões das fases, NA ORDEM:
    // botaoFase1, botaoFase2, botaoFase3, botaoFase4, botaoFase5
    public Button[] botoesFases;

    // Arraste aqui os cadeados, NA MESMA ORDEM dos botões:
    // o cadeado que está dentro do botaoFase1, depois o do botaoFase2, e assim por diante
    public GameObject[] cadeados;

    void Start()
    {
        // Lê até onde o jogador já liberou (se nunca jogou, só a fase 1)
        int faseLiberada = PlayerPrefs.GetInt("faseDesbloqueada", 1);

        for (int i = 0; i < botoesFases.Length; i++)
        {
            // posição 0 = fase 1, posição 1 = fase 2, ...
            bool liberada = (i + 1) <= faseLiberada;

            // o botão só fica clicável se a fase já foi liberada
            botoesFases[i].interactable = liberada;

            // o cadeado aparece só nas fases trancadas
            if (i < cadeados.Length && cadeados[i] != null)
            {
                cadeados[i].SetActive(!liberada);
            }
        }
    }

    // Atalhos só para testar: no Inspector, clique nos três pontinhos
    // do componente TrancaFases e escolha uma das opções.
    [ContextMenu("Liberar todas as fases")]
    void LiberarTodas()
    {
        PlayerPrefs.SetInt("faseDesbloqueada", 5);
        PlayerPrefs.Save();
    }

    [ContextMenu("Resetar progresso")]
    void Resetar()
    {
        PlayerPrefs.SetInt("faseDesbloqueada", 1);
        PlayerPrefs.Save();
    }
}
