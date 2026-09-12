using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Tutorial 04 - secao 2.4 (Porta de Correr)
// Habilita a area de teleporte do lado de fora do laboratorio
// quando a porta de correr estiver suficientemente aberta.
//
// Trabalho 1 - RN05: ao abrir, avisa o PuzzleSala2 para tirar o Outline.
public class EventosPortaCorrer : MonoBehaviour
{
    private bool isOpen = false;
    private ConfigurableJoint joint;

    public TeleportationArea teleporte;

    [Tooltip("Puzzle dos tres botoes: avisado quando a porta abre, para o Outline sumir.")]
    public PuzzleSala2 puzzle;

    [Tooltip("Deslocamento minimo (em metros) para considerar a porta aberta.")]
    public float aberturaMinima = 0.6f;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
    }

    float GetJointLinearX()
    {
        // Calcula posicao do anchor no mundo
        Vector3 worldAnchor = joint.transform.TransformPoint(joint.anchor);
        Vector3 connectedAnchor = joint.connectedAnchor;

        // Delta entre anchors
        Vector3 delta = worldAnchor - connectedAnchor;

        // Eixo X do joint no espaco global
        Vector3 axisX = joint.transform.TransformDirection(Vector3.right);

        // Projecao do deslocamento no eixo X
        float displacementX = Vector3.Dot(delta, axisX);

        return displacementX;
    }

    void Update()
    {
        if (joint == null)
            return;

        float abertura = Mathf.Abs(GetJointLinearX());

        // abriu
        if (!isOpen && abertura >= aberturaMinima)
        {
            isOpen = true;

            if (teleporte != null)
                teleporte.enabled = true;

            // RN05 - a porta abriu, o contorno nao e mais necessario
            if (puzzle != null)
                puzzle.PortaAbriu();
        }
        else
        {
            // Porta fechou
            if (isOpen && abertura < aberturaMinima)
            {
                isOpen = false;
                if (teleporte != null)
                    teleporte.enabled = false;
            }
        }
    }
}
