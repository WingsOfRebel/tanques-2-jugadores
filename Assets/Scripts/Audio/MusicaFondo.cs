using UnityEngine;

// Música de fondo (marcha fúnebre). Un solo objeto que sobrevive al cambio de escena,
// sin duplicarse. En la versión web empieza con la primera tecla o clic
// (los navegadores bloquean el audio hasta que el jugador interactúa).
[RequireComponent(typeof(AudioSource))]
public class MusicaFondo : MonoBehaviour
{
    public static MusicaFondo Instancia { get; private set; }

    private AudioSource m_Fuente;
    private bool m_Iniciada;

    private void Awake()
    {
        // Si ya existe otro objeto de música, este se destruye (evita duplicados)
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);

        m_Fuente = GetComponent<AudioSource>();
        m_Fuente.loop = true;
        m_Fuente.playOnAwake = false;
    }

    private void Start()
    {
#if !(UNITY_WEBGL && !UNITY_EDITOR)
        Iniciar(); // en editor y PC suena desde el principio
#endif
    }

    private void Update()
    {
        // En web: espera la primera tecla o clic
        if (!m_Iniciada && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            Iniciar();
    }

    private void Iniciar()
    {
        if (m_Iniciada || m_Fuente == null) return;
        m_Iniciada = true;
        if (m_Fuente.clip != null) m_Fuente.Play();
    }
}
