using UnityEngine;

// Explosión de la bomba (base del PDF). Cambios: también daña a los enemigos
// (capa "Enemigos") y avisa al GameManager para sumar puntos al jugador que disparó.
public class ShellExplosion : MonoBehaviour
{
    public LayerMask m_TankMask;                    // Capas afectadas ("Players"; "Enemigos" se añade sola)
    public ParticleSystem m_ExplosionParticles;     // Partículas de la explosión
    public AudioSource m_ExplosionAudio;            // Sonido de la explosión
    public float m_MaxDamage = 100f;                // Daño máximo (en el centro)
    public float m_ExplosionForce = 1000f;          // Fuerza de empuje
    public float m_MaxLifeTime = 2f;                // Segundos antes de destruirse sola
    public float m_ExplosionRadius = 5f;            // Radio de la explosión

    [HideInInspector] public int m_JugadorOrigen;   // Quién disparó (lo pone TankShooting)

    private bool m_Exploto;

    private void Start()
    {
        // Añade la capa Enemigos a la máscara (si la capa existe)
        m_TankMask |= LayerMask.GetMask("Enemigos");
        Destroy(gameObject, m_MaxLifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_Exploto) return;
        m_Exploto = true;

        // Todos los colliders dentro del radio, filtrados por capa
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            Rigidbody targetRigidbody = colliders[i].attachedRigidbody;
            if (!targetRigidbody)
                continue;

            float damage = CalculateDamage(targetRigidbody.position);

            // ¿Es un enemigo?
            EnemigoPatrulla enemigo = targetRigidbody.GetComponent<EnemigoPatrulla>();
            if (enemigo)
            {
                enemigo.TakeDamage(damage, m_JugadorOrigen);
                continue;
            }

            // Si no, es un tanque: empuje y daño
            targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

            TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
            if (!targetHealth)
                continue;

            targetHealth.TakeDamage(damage);
        }

        // Separa las partículas de la bomba para que sobrevivan a su destrucción
        m_ExplosionParticles.transform.parent = null;
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();

        Destroy(m_ExplosionParticles.gameObject, m_ExplosionParticles.main.duration);
        Destroy(gameObject);
    }

    // Más daño cuanto más cerca del centro de la explosión
    private float CalculateDamage(Vector3 targetPosition)
    {
        Vector3 explosionToTarget = targetPosition - transform.position;
        float explosionDistance = explosionToTarget.magnitude;
        float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;
        float damage = relativeDistance * m_MaxDamage;
        return Mathf.Max(0f, damage);
    }
}
