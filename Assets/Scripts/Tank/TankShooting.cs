using UnityEngine;
using UnityEngine.UI;

// Disparo con carga (base del PDF). Cambios: Unity 6 (linearVelocity) y la bomba
// recuerda qué jugador la disparó, para poder sumar puntos al destruir enemigos.
public class TankShooting : MonoBehaviour
{
    public int m_PlayerNumber = 1;          // Jugador (lo configura TankManager)
    public Rigidbody m_Shell;               // Prefab de la bomba
    public Transform m_FireTransform;       // Punto de donde sale la bomba
    public Slider m_AimSlider;              // Barra que muestra la fuerza de lanzamiento
    public AudioSource m_ShootingAudio;     // Fuente de audio del disparo
    public AudioClip m_ChargingClip;        // Sonido al cargar
    public AudioClip m_FireClip;            // Sonido al disparar
    public float m_MinLaunchForce = 15f;    // Fuerza mínima
    public float m_MaxLaunchForce = 30f;    // Fuerza máxima (carga completa)
    public float m_MaxChargeTime = 0.75f;   // Tiempo para llegar a la fuerza máxima

    private string m_FireButton;            // Eje de disparo (Fire1 / Fire2)
    private float m_CurrentLaunchForce;
    private float m_ChargeSpeed;
    private bool m_Fired;

    private void OnEnable()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        if (m_AimSlider != null) m_AimSlider.value = m_MinLaunchForce;
    }

    private void Start()
    {
        m_FireButton = "Fire" + m_PlayerNumber;
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }

    private void Update()
    {
        if (m_AimSlider != null) m_AimSlider.value = m_MinLaunchForce;

        // Si llegó a la fuerza máxima y aún no disparó: dispara
        if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
        {
            m_CurrentLaunchForce = m_MaxLaunchForce;
            Fire();
        }
        // Se acaba de pulsar el botón: empieza a cargar
        else if (Input.GetButtonDown(m_FireButton))
        {
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();
        }
        // Se mantiene pulsado: aumenta la fuerza
        else if (Input.GetButton(m_FireButton) && !m_Fired)
        {
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
            if (m_AimSlider != null) m_AimSlider.value = m_CurrentLaunchForce;
        }
        // Se soltó el botón: dispara
        else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
        {
            Fire();
        }
    }

    private void Fire()
    {
        m_Fired = true;

        // Crea la bomba y le da velocidad hacia adelante
        Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);
        shellInstance.linearVelocity = m_CurrentLaunchForce * m_FireTransform.forward;

        // La bomba recuerda quién la disparó (para el puntaje por enemigo destruido)
        ShellExplosion explosion = shellInstance.GetComponent<ShellExplosion>();
        if (explosion != null) explosion.m_JugadorOrigen = m_PlayerNumber;

        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();

        // Por precaución ante eventos de botón "perdidos"
        m_CurrentLaunchForce = m_MinLaunchForce;
    }
}
