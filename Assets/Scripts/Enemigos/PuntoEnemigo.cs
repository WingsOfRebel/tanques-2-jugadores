using UnityEngine;

// Marca dónde aparece un enemigo y por dónde patrulla.
// Patrulla en línea recta a lo largo del eje rojo (X) del objeto, +/- m_Distancia.
// Gira el objeto para cambiar la dirección de la patrulla.
public class PuntoEnemigo : MonoBehaviour
{
    public float m_Distancia = 4f;

    // Línea naranja en la escena para ver el recorrido
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.45f, 0f);
        Vector3 d = transform.right * m_Distancia;
        Gizmos.DrawLine(transform.position - d, transform.position + d);
        Gizmos.DrawWireSphere(transform.position - d, 0.5f);
        Gizmos.DrawWireSphere(transform.position + d, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}
