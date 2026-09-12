using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Trabalho 1
// Botao de reiniciar. Usado no painel de game over e tambem no totem que fica
// do lado de fora do laboratorio, para o jogador poder recomecar depois de
// vencer. Segue o padrao do BotaoPressionado da sala 2.
public class BotaoReiniciar : MonoBehaviour
{
    void Start()
    {
        var interactable = GetComponent<XRBaseInteractable>();

        if (interactable != null)
            interactable.selectEntered.AddListener(x => Clicou());
    }

    public void Clicou()
    {
        print("Jogar novamente!");

        // o jogo pode estar pausado: sem isso a cena nova nasceria congelada
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
