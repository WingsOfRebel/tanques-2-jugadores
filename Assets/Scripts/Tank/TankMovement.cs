using UnityEngine;

// Movimiento del tanque (base del PDF del profesor, Unity 6).
public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1;          // Qué jugador es (lo configura TankManager)
    public float m_Speed = 12f;             // Velocidad al avanzar y retroceder
    public float m_TurnSpeed = 180f;        // Giro en grados por segundo
    public AudioSource m_MovementAudio;     // Audio del motor (distinto al del disparo)
    public AudioClip m_EngineIdling;        // Sonido con el tanque quieto
    public AudioClip m_EngineDriving;       // Sonido con el tanque en movimiento
    public float m_PitchRange = 0.2f;       // Variación del tono del motor

    private string m_MovementAxisName;      // Eje de avance (Vertical1 / Vertical2)
    private string m_TurnAxisName;          // Eje de giro (Horizontal1 / Horizontal2)
    private Rigidbody m_Rigidbody;
    private float m_MovementInputValue;
    private float m_TurnInputValue;
    private float m_OriginalPitch;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Al activarse, el tanque deja de ser kinemático para poder moverse
        m_Rigidbody.isKinematic = false;
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }

    private void OnDisable()
    {
        // Al desactivarse, se vuelve kinemático para que se quede quieto
        m_Rigidbody.isKinematic = true;
    }

    private void Start()
    {
        m_MovementAxisName = "Vertical" + m_PlayerNumber;
        m_TurnAxisName = "Horizontal" + m_PlayerNumber;
        m_OriginalPitch = m_MovementAudio.pitch;
    }

    private void Update()
    {
        m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        EngineAudio();
    }

    private void EngineAudio()
    {
        // Sin entrada: el tanque está quieto
        if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
        {
            if (m_MovementAudio.clip == m_EngineDriving)
            {
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else
        {
            // Con entrada: el tanque se mueve
            if (m_MovementAudio.clip == m_EngineIdling)
            {
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }

    private void FixedUpdate()
    {
        Move();
        Turn();
    }

    private void Move()
    {
        // Avance en la dirección del tanque, según entrada, velocidad y tiempo
        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }

    private void Turn()
    {
        // Grados a girar en el eje Y según entrada, velocidad y tiempo
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }
}
