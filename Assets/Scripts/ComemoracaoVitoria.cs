using UnityEngine;

// Trabalho 1 - RN06
// Comemoracao de vitoria: quando o jogador sai do laboratorio, a placa de saida
// passa a mostrar a mensagem de missao concluida, os fogos de artificio disparam
// e toca a fanfarra.
//
// A placa e um objeto fisico do cenario, plantado do lado de fora. Antes era um
// Canvas que aparecia colado na frente do jogador, o que ficava artificial.
//
// O ShowMessageNaArea (tutorial 04) zera o Time.timeScale para travar o jogo,
// entao tudo aqui roda em tempo nao escalonado - senao os fogos ficariam
// congelados no ar.
public class ComemoracaoVitoria : MonoBehaviour
{
    [Header("Placa de vitoria")]
    [Tooltip("Placa fisica do cenario (ou Canvas, no modo antigo).")]
    public GameObject placa;

    [Tooltip("Texto da placa. Na vitoria ele troca para a mensagem de missao concluida.")]
    public TMPro.TextMeshPro textoPlaca;

    [TextArea(3, 8)]
    [Tooltip("Mensagem que aparece na placa quando o jogador escapa.")]
    public string mensagemVitoria =
        "<size=150%><b>MISSAO CONCLUIDA</b></size>\nVoce escapou do laboratorio!";

    [TextArea(2, 5)]
    [Tooltip("O que a placa mostra antes da vitoria. Reaplicado no Start, para a cena nunca comecar com a mensagem de vitoria gravada.")]
    public string mensagemInicial = "<size=150%><b>SAIDA</b></size>";

    [Tooltip("Contorno da placa, aceso na vitoria.")]
    public Outline outlinePlaca;

    [Tooltip("Modo antigo: a placa e reposicionada na frente do jogador. Deixe desmarcado para usar a placa fixa do cenario.")]
    public bool seguirOJogador = false;

    [Tooltip("Camera do jogador. A placa e posicionada na frente dela.")]
    public Transform cameraJogador;

    [Tooltip("A que distancia da camera a placa aparece.")]
    public float distancia = 2.2f;

    [Tooltip("Deslocamento vertical da placa em relacao a altura dos olhos.")]
    public float altura = -0.1f;

    [Header("Fogos de artificio")]
    public ParticleSystem[] fogos;

    [Header("Audio")]
    public AudioSource fanfarra;

    private bool jaComemorou = false;

    void Start()
    {
        // so esconde a placa no modo antigo: a placa do cenario fica sempre visivel
        if (placa != null && seguirOJogador)
            placa.SetActive(false);

        if (outlinePlaca != null)
            outlinePlaca.OutlineWidth = 0f;

        // a cena pode ter sido salva com a vitoria ja disparada: devolve a placa
        // ao texto normal, senao o jogo comeca parecendo que ja foi vencido
        if (textoPlaca != null)
            textoPlaca.text = mensagemInicial;

        // os fogos so aparecem na hora da vitoria
        foreach (var f in fogos)
        {
            if (f == null)
                continue;

            f.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            f.Clear(true);
            f.gameObject.SetActive(false);
        }
    }

    public void Comemorar()
    {
        if (jaComemorou)
            return;

        jaComemorou = true;

        if (seguirOJogador)
            PosicionarPlaca();
        else if (placa != null)
            placa.SetActive(true);

        // a placa de saida passa a anunciar a vitoria
        if (textoPlaca != null)
            textoPlaca.text = mensagemVitoria;

        if (outlinePlaca != null)
            outlinePlaca.OutlineWidth = 6f;

        foreach (var f in fogos)
        {
            if (f == null)
                continue;

            f.gameObject.SetActive(true);
            f.Play(true);
        }

        if (fanfarra != null)
            fanfarra.Play();

        print("MISSAO CONCLUIDA! Comemoracao iniciada.");
    }

    // Coloca a placa na frente do jogador, na altura dos olhos, encarando ele.
    // O Canvas do tutorial ficava parado dentro da sala 1, onde o jogador nem
    // chega a estar quando termina o jogo.
    private void PosicionarPlaca()
    {
        if (placa == null)
            return;

        if (cameraJogador != null)
        {
            // direcao para onde o jogador olha, sem inclinar a placa
            Vector3 frente = cameraJogador.forward;
            frente.y = 0f;

            if (frente.sqrMagnitude < 0.001f)
                frente = Vector3.forward;

            frente.Normalize();

            placa.transform.position = cameraJogador.position + frente * distancia + Vector3.up * altura;
            placa.transform.rotation = Quaternion.LookRotation(frente, Vector3.up);
        }

        placa.SetActive(true);
    }
}
