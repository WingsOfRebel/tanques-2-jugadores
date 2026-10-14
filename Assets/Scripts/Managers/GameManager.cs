using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Centro del juego. Base del PDF + requisitos de la Semana 4:
//  RF-04 la partida termina cuando un jugador pierde 5 rondas (intentos perdidos)
//  RF-05 pantalla final con ganador, puntaje y tiempo
//  RF-06 reloj de 240 s para toda la partida; al llegar a 0 ambos pierden
//  RF-07 enemigos que patrullan y estorban (se crean en los PuntoEnemigo)
public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Reglas")]
    public int m_MaxIntentosPerdidos = 5;       // Intentos perdidos que causan la derrota
    public float m_TiempoTotal = 240f;          // Reloj de la partida (segundos)
    public float m_StartDelay = 2f;             // Espera antes de cada ronda
    public float m_EndDelay = 2f;               // Espera al terminar cada ronda
    public int m_PuntosPorRonda = 100;          // Puntos por ganar una ronda
    public int m_PuntosPorEnemigo = 25;         // Puntos por destruir un enemigo

    [Header("Referencias")]
    public CameraControl m_CameraControl;       // CameraRig
    public GameObject m_TankPrefab;             // Prefab del tanque
    public TankManager[] m_Tanks;               // Un TankManager por jugador
    public GameObject m_EnemigoPrefab;          // Prefab del enemigo (opcional, hay uno de respaldo)
    public HUDJuego m_Hud;                      // Interfaz (se crea sola si falta)

    // Los enemigos solo se mueven y dañan mientras la ronda está activa
    public bool RondaActiva { get; private set; }

    private int m_RoundNumber;
    private WaitForSeconds m_StartWait;
    private WaitForSeconds m_EndWait;
    private float m_TiempoRestante;
    private bool m_TiempoAgotado;
    private bool m_PartidaTerminada;
    private TankManager m_RoundWinner;
    private TankManager m_GameWinner;
    private readonly List<GameObject> m_Enemigos = new List<GameObject>();

    private void Awake()
    {
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);

        PrepararHud();
        SpawnAllTanks();
        SetCameraTargets();
        NuevaPartida();
    }

    private void PrepararHud()
    {
        if (m_Hud == null) m_Hud = FindFirstObjectByType<HUDJuego>();
        if (m_Hud == null) m_Hud = new GameObject("HUD").AddComponent<HUDJuego>();
        m_Hud.m_AlReiniciar = ReiniciarPartida;
    }

    private void SpawnAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Instance = Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation);
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].Setup();
        }
    }

    private void SetCameraTargets()
    {
        Transform[] targets = new Transform[m_Tanks.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = m_Tanks[i].m_Instance.transform;
        }
        m_CameraControl.m_Targets = targets;
    }

    // Empieza (o reinicia) una partida completa
    private void NuevaPartida()
    {
        StopAllCoroutines();

        m_TiempoRestante = m_TiempoTotal;
        m_RoundNumber = 0;
        m_TiempoAgotado = false;
        m_PartidaTerminada = false;
        m_RoundWinner = null;
        m_GameWinner = null;
        RondaActiva = false;

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].ReiniciarMarcadores();
        }

        m_Hud.OcultarFinal();
        m_Hud.ActualizarReloj(m_TiempoRestante);
        ActualizarHud();

        StartCoroutine(GameLoop());
    }

    // Botón "Reiniciar" del panel final (o tecla R)
    public void ReiniciarPartida()
    {
        // Quita bombas en vuelo y enemigos de la partida anterior
        ShellExplosion[] bombas = FindObjectsByType<ShellExplosion>();
        for (int i = 0; i < bombas.Length; i++)
        {
            Destroy(bombas[i].gameObject);
        }
        BorrarEnemigos();

        NuevaPartida();
    }

    private IEnumerator GameLoop()
    {
        while (true)
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            if (m_PartidaTerminada)
            {
                MostrarPantallaFinal();
                yield break;
            }
        }
    }

    private IEnumerator RoundStarting()
    {
        // Reinicia tanques y enemigos, y bloquea los controles
        ResetAllTanks();
        CrearEnemigos();
        DisableTankControl();
        RondaActiva = false;

        m_CameraControl.SetStartPositionAndSize();

        m_RoundNumber++;
        m_Hud.MostrarMensaje("RONDA " + m_RoundNumber);

        yield return m_StartWait;
    }

    private IEnumerator RoundPlaying()
    {
        EnableTankControl();
        RondaActiva = true;
        m_Hud.MostrarMensaje(string.Empty);

        // La ronda sigue mientras haya 2 tanques y quede tiempo.
        // El reloj solo corre aquí, mientras se juega.
        while (!OneTankLeft() && m_TiempoRestante > 0f)
        {
            m_TiempoRestante -= Time.deltaTime;
            m_Hud.ActualizarReloj(m_TiempoRestante);
            yield return null;
        }

        m_TiempoRestante = Mathf.Max(0f, m_TiempoRestante);
        m_Hud.ActualizarReloj(m_TiempoRestante);

        // Si el tiempo se acabó y nadie ganó la ronda, ambos pierden
        m_TiempoAgotado = !OneTankLeft() && m_TiempoRestante <= 0f;
        RondaActiva = false;
    }

    private IEnumerator RoundEnding()
    {
        DisableTankControl();
        RondaActiva = false;
        m_RoundWinner = null;

        if (m_TiempoAgotado)
        {
            // RF-06: sin ganador
            m_PartidaTerminada = true;
            m_Hud.MostrarMensaje("TIEMPO AGOTADO\nAMBOS PIERDEN");
        }
        else
        {
            // Quien sigue en pie gana la ronda
            m_RoundWinner = GetRoundWinner();
            if (m_RoundWinner != null)
            {
                m_RoundWinner.m_Wins++;
                m_RoundWinner.m_Score += m_PuntosPorRonda;
            }

            // RF-04: cada tanque destruido pierde un intento (si ambos, pierden los dos)
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (!m_Tanks[i].m_Instance.activeSelf)
                    m_Tanks[i].m_Losses++;
            }

            EvaluarFinDePartida();
            m_Hud.MostrarMensaje(EndMessage());
        }

        ActualizarHud();
        yield return m_EndWait;
    }

    // ¿Queda uno o ninguno?
    private bool OneTankLeft()
    {
        int numTanksLeft = 0;
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                numTanksLeft++;
        }
        return numTanksLeft <= 1;
    }

    // Ganador de la ronda: el único tanque activo (null si ambos fueron destruidos)
    private TankManager GetRoundWinner()
    {
        TankManager vivo = null;
        int cuantos = 0;
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
            {
                vivo = m_Tanks[i];
                cuantos++;
            }
        }
        return cuantos == 1 ? vivo : null;
    }

    // RF-04: ¿alguien llegó al máximo de intentos perdidos?
    private void EvaluarFinDePartida()
    {
        bool alguienLlego = false;
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Losses >= m_MaxIntentosPerdidos)
                alguienLlego = true;
        }
        if (!alguienLlego) return;

        m_PartidaTerminada = true;

        // Gana el que NO llegó al máximo
        TankManager sobreviviente = null;
        int sinLlegar = 0;
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Losses < m_MaxIntentosPerdidos)
            {
                sobreviviente = m_Tanks[i];
                sinLlegar++;
            }
        }

        if (sinLlegar == 1)
        {
            m_GameWinner = sobreviviente;
        }
        else
        {
            // Ambos llegaron a la vez: gana el de mayor puntaje (si empatan, no hay ganador)
            TankManager mejor = null;
            bool empate = false;
            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (mejor == null || m_Tanks[i].m_Score > mejor.m_Score)
                {
                    mejor = m_Tanks[i];
                    empate = false;
                }
                else if (m_Tanks[i].m_Score == mejor.m_Score)
                {
                    empate = true;
                }
            }
            m_GameWinner = empate ? null : mejor;
        }
    }

    // Mensaje al final de cada ronda
    private string EndMessage()
    {
        string message = "EMPATE DE RONDA";
        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " GANA LA RONDA";

        message += "\n\n";

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Losses + " / " + m_MaxIntentosPerdidos + " intentos perdidos\n";
        }
        return message;
    }

    // RF-05: panel final con ganador (nombre y color), puntaje y tiempo jugado
    private void MostrarPantallaFinal()
    {
        float jugado = m_TiempoTotal - m_TiempoRestante;
        string titulo;
        string datos;

        if (m_TiempoAgotado)
        {
            titulo = "TIEMPO AGOTADO\nAMBOS PIERDEN";
            datos = "Tiempo jugado: " + FormatoTiempo(jugado);
        }
        else if (m_GameWinner != null)
        {
            titulo = m_GameWinner.m_ColoredPlayerText + "\n¡GANA LA PARTIDA!";
            datos = "Puntaje: " + m_GameWinner.m_Score + "\nTiempo jugado: " + FormatoTiempo(jugado);
        }
        else
        {
            titulo = "EMPATE";
            datos = "Ambos llegaron a " + m_MaxIntentosPerdidos + " intentos perdidos con el mismo puntaje\nTiempo jugado: " + FormatoTiempo(jugado);
        }

        datos += "\n\n";
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            datos += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Score + " puntos, "
                     + m_Tanks[i].m_Losses + " intentos perdidos\n";
        }

        m_Hud.MostrarMensaje(string.Empty);
        m_Hud.MostrarFinal(titulo, datos);
    }

    // Lo llama el enemigo al morir: suma puntos al jugador que lo destruyó
    public void EnemigoDestruido(int jugador)
    {
        if (!RondaActiva) return;
        if (jugador < 1 || jugador > m_Tanks.Length) return;

        m_Tanks[jugador - 1].m_Score += m_PuntosPorEnemigo;
        ActualizarHud();
    }

    private void ActualizarHud()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Hud.ActualizarJugador(i + 1, m_Tanks[i].m_ColoredPlayerText, m_Tanks[i].m_Losses, m_MaxIntentosPerdidos, m_Tanks[i].m_Score);
        }
    }

    private static string FormatoTiempo(float segundos)
    {
        int t = Mathf.RoundToInt(segundos);
        return (t / 60) + ":" + (t % 60).ToString("00");
    }

    // ---- Enemigos (RF-07) ----

    // Crea un enemigo en cada PuntoEnemigo de la escena
    private void CrearEnemigos()
    {
        BorrarEnemigos();

        PuntoEnemigo[] puntos = FindObjectsByType<PuntoEnemigo>();
        for (int i = 0; i < puntos.Length; i++)
        {
            GameObject go;
            if (m_EnemigoPrefab != null)
                go = Instantiate(m_EnemigoPrefab, puntos[i].transform.position, Quaternion.identity);
            else
                go = EnemigoPatrulla.Construir(null); // respaldo si no hay prefab

            EnemigoPatrulla enemigo = go.GetComponent<EnemigoPatrulla>();
            if (enemigo == null) enemigo = go.AddComponent<EnemigoPatrulla>();

            Vector3 direccion = puntos[i].transform.right;
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.01f) direccion = Vector3.right;

            enemigo.Configurar(puntos[i].transform.position, direccion.normalized, puntos[i].m_Distancia);
            m_Enemigos.Add(go);
        }
    }

    private void BorrarEnemigos()
    {
        for (int i = 0; i < m_Enemigos.Count; i++)
        {
            if (m_Enemigos[i] != null) Destroy(m_Enemigos[i]);
        }
        m_Enemigos.Clear();
    }

    // ---- Control de los tanques ----

    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].Reset();
    }

    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].EnableControl();
    }

    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++) m_Tanks[i].DisableControl();
    }
}
