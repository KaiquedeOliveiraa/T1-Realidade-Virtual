using System.Collections.Generic;
using UnityEngine;

// Trabalho 1 - RN01 e RN02
// Controla o liquido dentro do copo de bequer: o nivel (RN01) e a cor (RN02).
//
// RN01: os tres tubos despejam aqui por Receber(). A capacidade e dimensionada
// para caber exatamente a soma dos tres, entao eles enchem o copo sem passar do limite.
//
// RN04: alem da cor, guarda QUAIS tubos (rotulos A, B, C) ja foram derramados,
// que e o que o PuzzleSala1 compara com a formula da lousa.
//
// RN02: cada tubo registra a sua cor por AdicionarCor() na primeira vez que derrama.
// A cor do copo e a combinacao de todos os tubos ja derramados: com um tubo fica a
// cor dele, com dois a mistura dos dois, com tres a mistura dos tres. Sem proporcao
// de volume - a mistura e simples, como pede o enunciado.
public class ConteudoBequer : MonoBehaviour
{
    [Tooltip("Objeto 'Beaker liquid' de dentro do copo. O nivel e o Y do scale dele.")]
    public Transform liquido;

    [Tooltip("Nivel maximo (Y do scale) que o liquido pode alcancar no copo.")]
    public float capacidade = 0.9f;

    [Header("Aparencia do liquido no copo")]
    [Tooltip("Deixa o liquido opaco. O material do pacote vem transparente e sem escrever no depth, o que deixa a cor lavada atras do vidro do copo.")]
    public bool liquidoOpaco = true;

    [Tooltip("O material do pacote vem quase espelhado (metallic 0.91), o que faz o liquido refletir o ceu em vez de mostrar a cor.")]
    [Range(0f, 1f)]
    public float metalico = 0f;

    [Range(0f, 1f)]
    public float brilho = 0.25f;

    // quanto de liquido ja tem no copo, no mesmo Y do scale de 'liquido'
    public float NivelAtual { get; private set; }

    [Header("RN04 - puzzle da sala 1")]
    [Tooltip("Avisado a cada tubo derramado, para conferir se a mistura bate com a formula.")]
    public PuzzleSala1 puzzle;

    // cor resultante da mistura que esta no copo agora
    public Color CorAtual { get; private set; }

    // rotulos dos tubos ja derramados, na ordem em que entraram
    public List<string> RotulosDerramados { get { return rotulos; } }

    // quantos tubos ja foram derramados aqui
    public int QuantidadeDeTubos { get { return coresBase.Count; } }

    private readonly List<Color> coresBase = new List<Color>();
    private readonly List<Color> coresEmissao = new List<Color>();
    private readonly List<string> rotulos = new List<string>();

    // cor original do material, para poder voltar ao estado limpo
    private Color corOriginalBase;
    private Color corOriginalEmissao;

    private Renderer render;
    private Material materialInstancia;

    void Awake()
    {
        if (liquido == null)
            return;

        render = liquido.GetComponent<Renderer>();
        if (render == null || render.sharedMaterial == null)
            return;

        // copia do material para pintar so este copo: mexer no material original
        // sujaria o asset do pacote, que e compartilhado com os tubos de ensaio
        materialInstancia = new Material(render.sharedMaterial);
        materialInstancia.name = render.sharedMaterial.name + " (instancia do bequer)";
        corOriginalBase = materialInstancia.GetColor("_BaseColor");
        corOriginalEmissao = materialInstancia.GetColor("_EmissionColor");
        AjustarAparencia(materialInstancia);
        render.material = materialInstancia;
    }

    // Deixa a cor do liquido legivel dentro do copo. Mexe so na copia.
    private void AjustarAparencia(Material m)
    {
        m.SetFloat("_Metallic", metalico);
        m.SetFloat("_Smoothness", brilho);

        if (!liquidoOpaco)
            return;

        // receita padrao do URP para virar Opaque
        m.SetFloat("_Surface", 0f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_ZWrite", 1f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }

    void Start()
    {
        NivelAtual = 0f;
        AplicarNivel();
    }

    void OnDestroy()
    {
        if (materialInstancia != null)
            Destroy(materialInstancia);
    }

    // RN01 - recebe liquido de um tubo de ensaio.
    // Devolve quanto realmente coube no copo (pode ser menos que o pedido).
    public float Receber(float quantidade)
    {
        float aceito = Mathf.Min(quantidade, capacidade - NivelAtual);

        if (aceito <= 0f)
            return 0f;

        NivelAtual += aceito;
        AplicarNivel();

        return aceito;
    }

    // RN02 e RN04 - registra mais um tubo: a cor entra na mistura e o rotulo
    // entra na lista que o puzzle confere. Cada tubo chama isso uma unica vez.
    public void AdicionarCor(string rotulo, Color corBase, Color corEmissao)
    {
        coresBase.Add(corBase);
        coresEmissao.Add(corEmissao);
        rotulos.Add(rotulo);

        AplicarCor();

        if (puzzle != null)
            puzzle.VerificarMistura();
    }

    // RN04 - devolve o copo ao estado inicial: vazio, incolor e sem tubos registrados.
    public void Limpar()
    {
        NivelAtual = 0f;
        coresBase.Clear();
        coresEmissao.Clear();
        rotulos.Clear();

        AplicarNivel();

        if (materialInstancia != null)
        {
            materialInstancia.SetColor("_BaseColor", corOriginalBase);
            materialInstancia.SetColor("_Color", corOriginalBase);
            materialInstancia.SetColor("_EmissionColor", corOriginalEmissao);
        }

        CorAtual = corOriginalBase;
        print("Bequer esvaziado.");
    }

    private void AplicarNivel()
    {
        if (liquido == null)
            return;

        Vector3 escala = liquido.localScale;
        escala.y = NivelAtual;
        liquido.localScale = escala;

        // o 'Beaker liquid' comeca desabilitado: so aparece quando tem liquido
        liquido.gameObject.SetActive(NivelAtual > 0f);
    }

    private void AplicarCor()
    {
        if (materialInstancia == null || coresBase.Count == 0)
            return;

        Color corBase = Media(coresBase);
        Color corEmissao = Media(coresEmissao);

        materialInstancia.SetColor("_BaseColor", corBase);
        materialInstancia.SetColor("_Color", corBase);
        materialInstancia.SetColor("_EmissionColor", corEmissao);
        materialInstancia.EnableKeyword("_EMISSION");

        CorAtual = corBase;

        print("Mistura de " + coresBase.Count + " tubo(s) no bequer: #"
            + ColorUtility.ToHtmlStringRGB(corBase));
    }

    // mistura simples: a media das cores dos tubos derramados
    private static Color Media(List<Color> cores)
    {
        Color soma = new Color(0f, 0f, 0f, 0f);

        foreach (Color c in cores)
            soma += c;

        return soma / cores.Count;
    }
}
