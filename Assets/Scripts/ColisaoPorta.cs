using UnityEngine;

// Trabalho 1 - correcao de fisica da porta de batente.
//
// O tutorial 03 colocou o Espelho no layer Obstaculos e desmarcou
// Default x Obstaculos na matriz de colisao, para a porta nao bater nos
// caixilhos. So que o tutorial 04 moveu o Grab para o Trinco1, que ganhou
// Rigidbody proprio e ficou no layer Default - ou seja, o trinco voltou a
// colidir com os caixilhos e a porta trava e pula ao ser aberta.
//
// Mudar o layer do trinco resolveria, mas mexeria no que o Near-Far Interactor
// enxerga. Entao aqui a colisao e desligada so entre os colliders envolvidos,
// sem tocar em layer nenhum.
public class ColisaoPorta : MonoBehaviour
{
    [Tooltip("Colliders que se movem com a porta (trincos).")]
    public Collider[] partesDaPorta;

    [Tooltip("Colliders parados do batente (caixilhos).")]
    public Collider[] partesDoBatente;

    void Start()
    {
        if (partesDaPorta == null || partesDoBatente == null)
            return;

        int pares = 0;

        foreach (var porta in partesDaPorta)
        {
            if (porta == null)
                continue;

            foreach (var batente in partesDoBatente)
            {
                if (batente == null)
                    continue;

                Physics.IgnoreCollision(porta, batente, true);
                pares++;
            }
        }

        print("Colisao porta/batente desligada em " + pares + " par(es)");
    }
}
