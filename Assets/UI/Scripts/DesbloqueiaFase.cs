using UnityEngine;

// Coloque este script em cada tela de vitória (Vence F2, Vence F3, Vence F4).
// Quando a tela abre, ele libera a próxima fase.
public class DesbloqueiaFase : MonoBehaviour
{
    // Número da fase que o jogador acabou de vencer
    // (2 na Vence F2, 3 na Vence F3, e assim por diante)
    public int faseConcluida;

    void Start()
    {
        // Lê até onde o jogador já liberou (se nunca jogou, começa na fase 1)
        int faseLiberada = PlayerPrefs.GetInt("faseDesbloqueada", 1);

        // Só salva se a próxima fase ainda estiver trancada,
        // assim rejogar uma fase antiga nunca diminui o progresso
        if (faseConcluida + 1 > faseLiberada)
        {
            PlayerPrefs.SetInt("faseDesbloqueada", faseConcluida + 1);
            PlayerPrefs.Save();
        }
    }
}
