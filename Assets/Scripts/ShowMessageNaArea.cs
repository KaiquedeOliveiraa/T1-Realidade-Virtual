using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Tutorial 04 - secao 3 (Saindo do laboratorio)
// Quando o jogador pisa nesta area, mostra a mensagem de vitoria,
// pausa o jogo e desliga locomocao e linhas de teleporte.
//
// Trabalho 1 - RN06: em vez de so ligar o Canvas, dispara a ComemoracaoVitoria,
// que posiciona a placa na frente do jogador e solta os fogos de artificio.
public class ShowMessageNaArea : MonoBehaviour
{
    public Transform playerCamera;
    public GameObject messageObject;
    public LocomotionMediator locomotionSystem;

    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;

    public float maxDistance = 3f;

    [Tooltip("RN06 - comemoracao (placa + fogos + fanfarra). Se ficar vazio, cai no comportamento do tutorial, so ligando o messageObject.")]
    public ComemoracaoVitoria comemoracao;

    [Tooltip("Comportamento do tutorial: pausa o jogo e corta a locomocao ao vencer. Desmarque para o jogador poder caminhar e ver a comemoracao.")]
    public bool travarJogador = true;

    private bool triggered = false;

    void Update()
    {
        if (triggered || playerCamera == null)
            return;

        Ray ray = new Ray(playerCamera.position, Vector3.down);

        // RaycastAll em vez de Raycast: o piso de grama do cenario externo pode
        // ficar entre a camera e esta area, e com o Raycast simples so o primeiro
        // acerto contava - a vitoria nunca disparava.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == gameObject)
            {
                // RN06 - a comemoracao cuida da placa; sem ela, comportamento do tutorial
                if (comemoracao != null)
                    comemoracao.Comemorar();
                else if (messageObject != null)
                    messageObject.SetActive(true);

                // RN06 - com travarJogador desmarcado o jogador continua andando
                // pelo lado de fora para ver os fogos e ler a placa
                if (travarJogador)
                {
                    Time.timeScale = 0; // pausar o jogo

                    if (locomotionSystem != null)
                        locomotionSystem.enabled = false; // nao conseguir mais andar

                    if (leftRay != null)
                        leftRay.enabled = false;  // desativa linha de teleporte do controle esquerdo
                    if (rightRay != null)
                        rightRay.enabled = false; // desativa linha de teleporte do controle direito
                }

                triggered = true;
                break;
            }
        }
    }

    // Time.timeScale eh global: garante que o editor nao fique travado
    // ao sair do playmode com o jogo pausado.
    private void OnDisable()
    {
        if (triggered && travarJogador)
            Time.timeScale = 1f;
    }
}
