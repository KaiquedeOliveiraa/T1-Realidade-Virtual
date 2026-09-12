using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Tutorial 03 - secao 2.2 (Poke Interactor)
// Trabalho 1 - RN05: alem de imprimir, o botao avisa o PuzzleSala2, que conta
// os toques no botao do meio.
public class BotaoPressionado : MonoBehaviour
{
    public int numBotao;

    [Tooltip("Gerenciador do puzzle da sala 2. Os tres botoes apontam para o mesmo.")]
    public PuzzleSala2 puzzle;

    void Start()
    {
        GetComponent<XRBaseInteractable>().selectEntered.AddListener(x => Pressionei());
    }

    public void Pressionei()
    {
        print("PRESS " + numBotao);

        if (puzzle != null)
            puzzle.Pressionou(numBotao);
    }
}
