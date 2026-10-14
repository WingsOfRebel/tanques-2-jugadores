using UnityEngine;
using UnityEngine.UI;

// Salud del tanque (base del PDF).
public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 100f;               // Salud inicial
    public Slider m_Slider;                             // Barra de salud
    public Image m_FillImage;                           // Relleno de la barra
    public Color m_FullHealthColor = Color.green;       // Color con salud completa
    public Color m_ZeroHealthColor = Color.red;         // Color con salud vacía
    public GameObject m_ExplosionPrefab;                // Explosión al morir

    private AudioSource m_ExplosionAudio;
    private ParticleSystem m_ExplosionParticles;
    private float m_CurrentHealth;
    private bool m_Dead;

    private void Awake()
    {
        // Se crea una sola vez y se reutiliza en cada muerte
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
        m_ExplosionParticles.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Al activarse, la salud vuelve al máximo
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;
        SetHealthUI();
    }

    public void TakeDamage(float amount)
    {
        m_CurrentHealth -= amount;
        SetHealthUI();

        if (m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }

    private void SetHealthUI()
    {
        m_Slider.value = m_CurrentHealth;
        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
    }

    private void OnDeath()
    {
        m_Dead = true;

        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);
        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();

        gameObject.SetActive(false);
    }
}
