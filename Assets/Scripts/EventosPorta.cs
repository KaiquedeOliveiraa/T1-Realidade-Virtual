using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

// Tutorial 03 - secao 3 (Teleporte) + Tutorial 04 - secao 2.3 (correcao do angulo)
// Habilita a area de teleporte da sala 2 (e o grab da porta de correr)
// quando a porta de batente estiver aberta.
//
// Trabalho 1 - RN05: o Grab da porta de correr virou condicao composta. Nao
// basta esta porta estar aberta: o puzzle dos botoes tambem precisa estar
// resolvido, senao a porta de correr continua trancada.
public class EventosPorta : MonoBehaviour
{
    private bool isOpen = false;
    private HingeJoint hinge;

    public TeleportationArea teleporte;
    public XRGrabInteractable grabPorta;

    [Tooltip("Puzzle dos tres botoes. Se ficar vazio, a porta de correr libera so com esta porta aberta, como era antes da RN05.")]
    public PuzzleSala2 puzzleSala2;

    [Tooltip("RN04 - puzzle da mistura. Avisado quando esta porta abre, para o Outline sumir.")]
    public PuzzleSala1 puzzleSala1;

    [Tooltip("Rigidbody do Hanger da porta de correr. Cinematico enquanto ela estiver trancada, senao da para empurrar.")]
    public Rigidbody corpoHangerCorrer;

    [Tooltip("Rigidbody do BarnDoor_01. Cinematico enquanto a porta estiver trancada.")]
    public Rigidbody corpoFolhaCorrer;

    void Start()
    {
        hinge = GetComponent<HingeJoint>();

        // comeca tudo fechado e travado
        AtualizarAcessos(false);
    }

    void Update()
    {
        if (hinge == null)
            return;

        float angle = hinge.angle;

        // abriu
        if (!isOpen && angle <= -40)
        {
            isOpen = true;
            AtualizarAcessos(true);

            // RN04 - a porta abriu, o contorno nao e mais necessario
            if (puzzleSala1 != null)
                puzzleSala1.PortaAbriu();
        }
        else
        {
            // Porta fechou
            if (isOpen && angle > -40)
            {
                isOpen = false;
                AtualizarAcessos(false);
            }
        }
    }

    // Chamado pelo PuzzleSala2 quando o puzzle e resolvido: a porta de batente
    // pode ja estar aberta, entao o acesso precisa ser recalculado na hora.
    public void ReavaliarAcessos()
    {
        AtualizarAcessos(isOpen);
    }

    // os testes de null permitem usar o script antes de o tutorial 04 estar concluido
    private void AtualizarAcessos(bool liberado)
    {
        if (teleporte != null)
            teleporte.enabled = liberado;

        // RN05 - o grab da porta de correr exige as duas condicoes
        bool liberaCorrer = liberado && (puzzleSala2 == null || puzzleSala2.Resolvido);

        if (grabPorta != null)
            grabPorta.enabled = liberaCorrer;

        // so desligar o Grab nao segura a porta: sem isso da para empurrar
        if (corpoHangerCorrer != null)
            corpoHangerCorrer.isKinematic = !liberaCorrer;

        if (corpoFolhaCorrer != null)
            corpoFolhaCorrer.isKinematic = !liberaCorrer;
    }
}
