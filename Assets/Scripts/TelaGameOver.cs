using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

// Trabalho 1 - RN04 (falha do puzzle)
// Quando o jogador derrama o reagente toxico no bequer, sai fumaca do copo,
// a tela escurece e aparece o painel de game over com o botao de jogar
// novamente, que recarrega a cena.
//
// Tudo roda em tempo nao escalonado, para funcionar mesmo se o jogo estiver
// pausado (Time.timeScale = 0).
public class TelaGameOver : MonoBehaviour
{
    [Header("Jogador")]
    [Tooltip("Main Camera do XR Origin.")]
    public Transform cameraJogador;

    [Header("Fumaca no bequer")]
    public ParticleSystem fumaca;

    [Tooltip("Quanto tempo a fumaca sobe antes da tela escurecer.")]
    public float atrasoAteEscurecer = 1.1f;

    [Header("Escurecimento da tela")]
    [Tooltip("Material transparente usado no veu que cobre a visao.")]
    public Material materialVeu;

    public float tempoEscurecendo = 1.3f;

    [Range(0f, 1f)]
    public float opacidadeFinal = 0.90f;

    [Header("Painel de game over")]
    public GameObject painel;

    [Tooltip("A que distancia da camera o painel fica preso.")]
    public float distanciaPainel = 0.75f;

    [Tooltip("Escala do painel nessa distancia, para o tamanho aparente ficar bom.")]
    public float escalaPainel = 0.40f;

    [Header("Desligar durante o game over")]
    [Tooltip("Objeto 'Locomotion' do XR Origin. Desligar so o LocomotionMediator nao para os providers, o jogador continuaria andando.")]
    public GameObject grupoLocomocao;

    public LocomotionMediator locomocao;
    public XRRayInteractor teleporteEsq;
    public XRRayInteractor teleporteDir;

    [Header("Audio")]
    public AudioSource somFalha;

    private Renderer veu;
    private Material veuInstancia;
    private bool jaMostrou = false;

    void Start()
    {
        if (painel != null)
            painel.SetActive(false);

        CriarVeu();
    }

    // O veu e um quad colado na camera. Precisa seguir a cabeca, senao o
    // jogador simplesmente olha para o lado e escapa do escurecimento.
    private void CriarVeu()
    {
        if (cameraJogador == null || materialVeu == null)
            return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "VeuGameOver";
        Destroy(go.GetComponent<Collider>());

        go.transform.SetParent(cameraJogador, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0.32f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

        veu = go.GetComponent<Renderer>();
        veuInstancia = new Material(materialVeu);
        veuInstancia.name = "VeuGameOver (instancia)";
        veu.material = veuInstancia;

        DefinirOpacidade(0f);
        veu.enabled = false;
    }

    public void Mostrar()
    {
        if (jaMostrou)
            return;

        jaMostrou = true;
        print("REACAO CONTAMINADA! Game over.");

        if (fumaca != null)
        {
            fumaca.gameObject.SetActive(true);
            fumaca.Play(true);
        }

        if (somFalha != null)
            somFalha.Play();

        StartCoroutine(Sequencia());
    }

    private IEnumerator Sequencia()
    {
        // deixa a fumaca subir antes de tampar a visao
        yield return new WaitForSecondsRealtime(atrasoAteEscurecer);

        DesligarControles();

        if (veu != null)
        {
            veu.enabled = true;

            float t = 0f;
            while (t < tempoEscurecendo)
            {
                t += Time.unscaledDeltaTime;
                DefinirOpacidade(Mathf.Lerp(0f, opacidadeFinal, t / tempoEscurecendo));
                yield return null;
            }
            DefinirOpacidade(opacidadeFinal);
        }

        PosicionarPainel();
    }

    private void DesligarControles()
    {
        // o Near-Far continua ligado: e com ele que o jogador aponta para o botao
        if (grupoLocomocao != null) grupoLocomocao.SetActive(false);
        if (locomocao != null) locomocao.enabled = false;
        if (teleporteEsq != null) teleporteEsq.enabled = false;
        if (teleporteDir != null) teleporteDir.enabled = false;
    }

    // O painel fica preso na camera. Antes ele era plantado no mundo e o jogador
    // simplesmente andava para o lado e seguia jogando depois de ter errado.
    private void PosicionarPainel()
    {
        if (painel == null)
            return;

        if (cameraJogador != null)
        {
            painel.transform.SetParent(cameraJogador, false);
            painel.transform.localPosition = new Vector3(0f, 0f, distanciaPainel);
            painel.transform.localRotation = Quaternion.identity;
            painel.transform.localScale = Vector3.one * escalaPainel;
        }

        painel.SetActive(true);
    }

    private void DefinirOpacidade(float a)
    {
        if (veuInstancia == null)
            return;

        Color c = veuInstancia.GetColor("_BaseColor");
        c.a = a;
        veuInstancia.SetColor("_BaseColor", c);
    }

    // Chamado pelo BotaoReiniciar.
    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (veuInstancia != null)
            Destroy(veuInstancia);
    }
}
