using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Trabalho 1 - RN04
// Puzzle da primeira sala: a lousa mostra a formula (A + C). O jogador tem que
// derramar exatamente esses dois reagentes no copo de bequer.
//
// Enquanto nao acertar, a porta de batente fica trancada de duas formas: o Grab
// do Trinco fica desabilitado (nao abre com o G) e os Rigidbody da folha e do
// trinco ficam cinematicos (nao abre no empurrao). So desligar o Grab nao basta:
// a fisica continua livre e o jogador consegue empurrar a porta com o corpo ou
// com um objeto na mao.
//
// Se o jogador derramar o reagente toxico (o que o cartaz manda evitar), a
// reacao e perdida: sai fumaca, a tela escurece e aparece o game over com o
// botao de jogar novamente.
public class PuzzleSala1 : MonoBehaviour
{
    [Header("Formula da lousa")]
    [Tooltip("Rotulos que precisam ser derramados no bequer. A ordem nao importa.")]
    public string[] formula = { "A", "C" };

    [Header("Referencias")]
    public ConteudoBequer bequer;

    [Tooltip("Os tres tubos, para reabastecer quando a mistura der errado.")]
    public DerramarLiquido[] tubos;

    [Header("Trava da porta de batente")]
    [Tooltip("XRGrabInteractable do Trinco1. Fica desabilitado ate o puzzle ser resolvido.")]
    public XRGrabInteractable trincoPorta;

    [Tooltip("Rigidbody do Espelho (folha da porta). Fica cinematico enquanto trancada.")]
    public Rigidbody corpoPorta;

    [Tooltip("Rigidbody do Trinco1. Fica cinematico enquanto trancada.")]
    public Rigidbody corpoTrinco;

    public Outline outlinePorta;
    public AudioSource somVitoria;

    [Tooltip("Brilho que sai do bequer quando a mistura da certo.")]
    public ParticleSystem brilhoSucesso;
    public float larguraOutline = 6f;

    [Header("Erro na mistura")]
    [Tooltip("Tela de game over acionada quando o reagente toxico entra no bequer.")]
    public TelaGameOver gameOver;

    public bool Resolvido { get; private set; }

    void Start()
    {
        Resolvido = false;

        TravarPorta(true);
        MostrarOutline(false);
    }

    // Chamado pelo ConteudoBequer sempre que um tubo e derramado.
    public void VerificarMistura()
    {
        if (Resolvido || bequer == null)
            return;

        List<string> derramados = bequer.RotulosDerramados;

        // algum reagente fora da formula? a mistura ja esta perdida
        foreach (string r in derramados)
        {
            if (!EstaNaFormula(r))
            {
                print("Reagente " + r + " nao faz parte da formula. Reacao contaminada!");

                if (gameOver != null)
                    gameOver.Mostrar();

                return;
            }
        }

        // ainda falta algum reagente da formula?
        if (derramados.Count < formula.Length)
        {
            print("Mistura: " + derramados.Count + " de " + formula.Length + " reagentes.");
            return;
        }

        Resolver();
    }

    private bool EstaNaFormula(string rotulo)
    {
        foreach (string f in formula)
            if (f == rotulo)
                return true;

        return false;
    }

    private void Resolver()
    {
        Resolvido = true;
        print("Puzzle da sala 1 resolvido! Porta de batente destrancada.");

        TravarPorta(false);
        MostrarOutline(true);

        // faiscas saindo do copo: a reacao deu certo
        if (brilhoSucesso != null)
        {
            brilhoSucesso.gameObject.SetActive(true);
            brilhoSucesso.Play(true);
        }

        if (somVitoria != null)
            somVitoria.Play();
    }

    // Trava a porta pelos dois caminhos: interacao e fisica.
    private void TravarPorta(bool travada)
    {
        if (trincoPorta != null)
            trincoPorta.enabled = !travada;

        if (corpoPorta != null)
            corpoPorta.isKinematic = travada;

        if (corpoTrinco != null)
            corpoTrinco.isKinematic = travada;
    }

    // Chamado pelo EventosPorta quando a porta de batente abre.
    public void PortaAbriu()
    {
        if (outlinePorta == null || outlinePorta.OutlineWidth <= 0f)
            return;

        MostrarOutline(false);
        print("Porta de batente aberta: Outline removido.");
    }

    private void MostrarOutline(bool ligado)
    {
        if (outlinePorta != null)
            outlinePorta.OutlineWidth = ligado ? larguraOutline : 0f;
    }
}
