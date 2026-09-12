using UnityEngine;

// Trabalho 1 - RN05
// Puzzle da segunda sala: o jogador precisa pressionar tres vezes o botao do
// meio. Enquanto nao conseguir, a porta de correr fica trancada (o Grab dela
// nao e liberado). Ao resolver, aparece o Outline na porta e toca o som de
// vitoria; o Outline some quando a porta e efetivamente aberta.
public class PuzzleSala2 : MonoBehaviour
{
    [Header("Regra do puzzle")]
    [Tooltip("Num Botao do botao do meio, conforme o componente BotaoPressionado.")]
    public int botaoCerto = 2;

    [Tooltip("Quantas vezes o botao do meio precisa ser pressionado.")]
    public int toquesNecessarios = 3;

    [Tooltip("Se marcado, encostar em um dos outros dois botoes zera a contagem.")]
    public bool outrosBotoesZeram = true;

    [Tooltip("Tempo minimo entre dois toques validos. O prefab do botao tem colisores sobrepostos, entao um unico toque pode disparar o evento mais de uma vez.")]
    public float intervaloMinimo = 0.2f;

    [Header("Recompensa")]
    [Tooltip("Outline da porta de correr (BarnDoor_01).")]
    public Outline outlinePorta;

    [Tooltip("Som tocado quando o puzzle e resolvido.")]
    public AudioSource somVitoria;

    [Tooltip("Largura do contorno quando o puzzle e resolvido.")]
    public float larguraOutline = 6f;

    [Header("Ligacao com a porta de batente")]
    [Tooltip("EventosPorta do Espelho. E reavaliado ao resolver, porque o Grab da porta de correr depende das duas condicoes: porta de batente aberta E puzzle resolvido.")]
    public EventosPorta eventosPortaBatente;

    // usado pelo EventosPorta para decidir se libera o Grab da porta de correr
    public bool Resolvido { get; private set; }

    private int toques;
    private float instanteUltimoToque = -99f;

    void Start()
    {
        Resolvido = false;
        toques = 0;
        MostrarOutline(false);
    }

    // Chamado pelo BotaoPressionado de cada um dos tres botoes.
    public void Pressionou(int numBotao)
    {
        if (Resolvido)
            return;

        // ignora repeticao do mesmo toque vinda de colisores sobrepostos
        if (Time.time - instanteUltimoToque < intervaloMinimo)
            return;

        instanteUltimoToque = Time.time;

        if (numBotao == botaoCerto)
        {
            toques++;
            print("Botao do meio: " + toques + " de " + toquesNecessarios);

            if (toques >= toquesNecessarios)
                Resolver();

            return;
        }

        if (outrosBotoesZeram && toques > 0)
        {
            toques = 0;
            print("Botao errado! Contagem zerada.");
        }
    }

    private void Resolver()
    {
        Resolvido = true;
        print("Puzzle da sala 2 resolvido! Porta de correr destrancada.");

        MostrarOutline(true);

        if (somVitoria != null)
            somVitoria.Play();

        // a porta de batente pode ja estar aberta: manda reavaliar o Grab
        if (eventosPortaBatente != null)
            eventosPortaBatente.ReavaliarAcessos();
    }

    // Chamado pelo EventosPortaCorrer quando a porta de correr abre.
    public void PortaAbriu()
    {
        if (outlinePorta == null || outlinePorta.OutlineWidth <= 0f)
            return;

        MostrarOutline(false);
        print("Porta de correr aberta: Outline removido.");
    }

    private void MostrarOutline(bool ligado)
    {
        if (outlinePorta != null)
            outlinePorta.OutlineWidth = ligado ? larguraOutline : 0f;
    }
}
