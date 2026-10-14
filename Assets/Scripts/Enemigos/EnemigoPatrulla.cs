using UnityEngine;

// Enemigo que va y viene (como el ObstaculoMovil de Roll a Ball, con Mathf.PingPong).
// Estorba a los tanques: los bloquea y les hace daño al tocarlos (con espera entre golpes).
// Las bombas lo destruyen y el jugador que lo destruye gana puntos.
[RequireComponent(typeof(Rigidbody))]
public class EnemigoPatrulla : MonoBehaviour
{
    public float m_Velocidad = 3f;              // Unidades por segundo
    public float m_Vida = 50f;
    public float m_DanoPorGolpe = 20f;          // Daño al tocar un tanque
    public float m_EsperaEntreGolpes = 1f;      // Segundos entre golpes

    private Rigidbody m_Rigidbody;
    private Vector3 m_Origen;
    private Vector3 m_Direccion = Vector3.right;
    private float m_Distancia = 4f;
    private float m_Fase;
    private float m_ProximoGolpe;
    private bool m_Muerto;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.isKinematic = true;   // se mueve por código, no por física
        m_Rigidbody.useGravity = false;
        m_Origen = transform.position;

        // Todas las partes del enemigo en la capa "Enemigos"
        int capa = LayerMask.NameToLayer("Enemigos");
        if (capa >= 0)
        {
            Transform[] partes = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < partes.Length; i++) partes[i].gameObject.layer = capa;
        }
    }

    // Lo llama el GameManager al crearlo
    public void Configurar(Vector3 origen, Vector3 direccion, float distancia)
    {
        m_Origen = origen;
        m_Direccion = direccion;
        m_Distancia = distancia;
        m_Fase = distancia; // empieza en el centro del recorrido
    }

    private void FixedUpdate()
    {
        if (!Activo()) return;

        // Va y viene entre -distancia y +distancia
        m_Fase += m_Velocidad * Time.fixedDeltaTime;
        float desplazamiento = Mathf.PingPong(m_Fase, m_Distancia * 2f) - m_Distancia;
        m_Rigidbody.MovePosition(m_Origen + m_Direccion * desplazamiento);

        // Giro lento, solo visual
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * Quaternion.Euler(0f, 90f * Time.fixedDeltaTime, 0f));
    }

    private void OnCollisionStay(Collision colision)
    {
        if (m_Muerto || !Activo() || Time.time < m_ProximoGolpe) return;

        TankHealth tanque = colision.collider.GetComponentInParent<TankHealth>();
        if (tanque == null) return;

        tanque.TakeDamage(m_DanoPorGolpe);
        m_ProximoGolpe = Time.time + m_EsperaEntreGolpes;
    }

    // Lo llama ShellExplosion
    public void TakeDamage(float cantidad, int jugadorOrigen)
    {
        if (m_Muerto) return;

        m_Vida -= cantidad;
        if (m_Vida <= 0f)
        {
            m_Muerto = true;
            if (GameManager.Instancia != null) GameManager.Instancia.EnemigoDestruido(jugadorOrigen);
            Destroy(gameObject);
        }
    }

    // Solo se mueve y daña mientras la ronda está activa
    private bool Activo()
    {
        return GameManager.Instancia == null || GameManager.Instancia.RondaActiva;
    }

    // Construye un enemigo con formas básicas (cilindro + esfera).
    // Lo usa la herramienta del editor para crear el prefab y el GameManager como respaldo.
    public static GameObject Construir(Material material)
    {
        GameObject raiz = new GameObject("Enemigo");

        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cuerpo.name = "Cuerpo";
        cuerpo.transform.SetParent(raiz.transform, false);
        cuerpo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        cuerpo.transform.localScale = new Vector3(1.6f, 0.9f, 1.6f);

        GameObject cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabeza.name = "Cabeza";
        cabeza.transform.SetParent(raiz.transform, false);
        cabeza.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        cabeza.transform.localScale = Vector3.one;

        // La cabeza es solo decoración: sin collider
        Collider colliderCabeza = cabeza.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(colliderCabeza); else DestroyImmediate(colliderCabeza);

        if (material != null)
        {
            cuerpo.GetComponent<Renderer>().sharedMaterial = material;
            cabeza.GetComponent<Renderer>().sharedMaterial = material;
        }

        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        raiz.AddComponent<EnemigoPatrulla>();
        return raiz;
    }
}
