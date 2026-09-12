using UnityEngine;

// Tutorial 02 - secao 2.1 (derramar) e 2.1.1 (contorno)
// Trabalho 1 - RN01: o derramamento passou a ser continuo. Enquanto o tubo
// estiver virado dentro da ZonaDerramar, ele perde liquido (Y do scale) e o
// copo de bequer ganha. Se o jogador desvirar o tubo ou tirar da zona, para;
// e pode voltar a derramar depois com o que sobrou no tubo.
//
// Trabalho 1 - RN02: na primeira vez que este tubo derrama, ele registra a sua
// cor no copo, que passa a mostrar a mistura de todos os tubos ja derramados.
//
// Trabalho 1 - RN03: o audio do liquido caindo toca em loop enquanto esta
// derramando e para assim que o jogador desvira o tubo ou ele esvazia.
//
// Trabalho 1 - RN04: cada tubo tem um 'rotulo' (A, B ou C) que aparece na
// etiqueta e e o que o puzzle usa para saber quais reagentes foram misturados.
public class DerramarLiquido : MonoBehaviour
{
    [Header("RN04 - identificacao do reagente")]
    [Tooltip("Letra deste tubo (A, B ou C). E o que aparece na etiqueta e o que o puzzle compara com a formula da lousa.")]
    public string rotulo = "A";

    [SerializeField]
    private float anguloMin = 100f;

    [Header("RN01 - transferencia de liquido")]
    [Tooltip("Objeto 'Glass_Lab_test_tube6' de dentro deste tubo. O nivel e o Y do scale dele.")]
    [SerializeField]
    private Transform liquidoTubo;

    [Tooltip("Copo de bequer que recebe o liquido.")]
    [SerializeField]
    private ConteudoBequer bequer;

    [Tooltip("Quanto o tubo esvazia por segundo, em unidades de Y do scale.")]
    [SerializeField]
    private float velocidadeDerramar = 0.4f;

    [Tooltip("Quanto este tubo, cheio, faz o liquido do bequer subir. A soma dos tres tubos nao pode passar da capacidade do copo.")]
    [SerializeField]
    private float rendimentoNoBequer = 0.3f;

    [Header("RN03 - audio")]
    [Tooltip("Som do liquido caindo. Toca em loop enquanto derrama.")]
    [SerializeField]
    private AudioSource audioDerramar;

    private bool inZonaDerramar = false;
    private bool estahDerramando = false;

    private float nivelTubo;
    private float nivelInicial;
    private float fatorConversao;

    private Renderer renderLiquido;
    private bool jaAdicionouCor = false;

    // quanto ainda tem no tubo, de 0 (vazio) a 1 (como comecou)
    public float FracaoRestante
    {
        get { return nivelInicial > 0f ? nivelTubo / nivelInicial : 0f; }
    }

    void Start()
    {
        nivelInicial = liquidoTubo != null ? liquidoTubo.localScale.y : 0f;
        nivelTubo = nivelInicial;

        // a cor deste tubo sai do proprio material do liquido, entao trocar o
        // material no Editor ja muda a cor que vai para a mistura
        renderLiquido = liquidoTubo != null ? liquidoTubo.GetComponent<Renderer>() : null;

        // converte "Y do scale do tubo" para "Y do scale do liquido do copo"
        fatorConversao = nivelInicial > 0f ? rendimentoNoBequer / nivelInicial : 0f;
    }

    void Update()
    {
        float angulo = Vector3.Angle(transform.up, Vector3.up);

        bool deveDerramar = inZonaDerramar
            && nivelTubo > 0f
            && angulo > anguloMin;

        if (deveDerramar)
            Pour();
        else if (estahDerramando)
            PararDeDerramar();
    }

    void Pour()
    {
        if (!estahDerramando)
        {
            estahDerramando = true;

            // RN03 - comeca o audio junto com o derramamento
            if (audioDerramar != null && !audioDerramar.isPlaying)
                audioDerramar.Play();

            print("Comecou a derramar!");
        }

        RegistrarCorNoBequer();

        // o que sai do tubo neste frame, sem deixar o nivel ficar negativo
        float saiu = Mathf.Min(velocidadeDerramar * Time.deltaTime, nivelTubo);

        nivelTubo -= saiu;
        AplicarNivelTubo();

        if (bequer != null)
            bequer.Receber(saiu * fatorConversao);

        if (nivelTubo <= 0f)
            print("Tubo vazio!");
    }

    // RN02 - avisa o copo da cor deste tubo, so na primeira vez que derrama
    void RegistrarCorNoBequer()
    {
        if (jaAdicionouCor || bequer == null || renderLiquido == null)
            return;

        jaAdicionouCor = true;

        Material m = renderLiquido.sharedMaterial;
        bequer.AdicionarCor(rotulo, m.GetColor("_BaseColor"), m.GetColor("_EmissionColor"));
    }

    void PararDeDerramar()
    {
        estahDerramando = false;

        // RN03 - o audio termina junto com o derramamento
        if (audioDerramar != null && audioDerramar.isPlaying)
            audioDerramar.Stop();

        print("Parou de derramar!");
    }

    // RN04 - enche o tubo de novo, para o jogador poder repetir a mistura
    // depois de descartar o conteudo errado do bequer.
    public void Reabastecer()
    {
        nivelTubo = nivelInicial;
        jaAdicionouCor = false;
        AplicarNivelTubo();
    }

    private void AplicarNivelTubo()
    {
        if (liquidoTubo == null)
            return;

        Vector3 escala = liquidoTubo.localScale;
        escala.y = nivelTubo;
        liquidoTubo.localScale = escala;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ZonaDerramar"))
        {
            gameObject.GetComponent<Outline>().OutlineWidth = 5f;
            inZonaDerramar = true;
            print("In ZonaDerramar!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ZonaDerramar"))
        {
            gameObject.GetComponent<Outline>().OutlineWidth = 0f;
            inZonaDerramar = false;
            print("Out ZonaDerramar!");
        }
    }
}
