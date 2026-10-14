using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Interfaz del juego, construida por código (no hay que arrastrar nada):
//  RF-01 nombre y matrícula arriba a la derecha
//  RF-04 intentos perdidos de cada jugador (0 a 5)
//  RF-05 panel final con ganador, puntaje y tiempo
//  RF-06 reloj de la partida (mm:ss)
public class HUDJuego : MonoBehaviour
{
    public string m_NombreMatricula = "Edwin Flete - 1-22-1805";
    public System.Action m_AlReiniciar;     // Lo asigna el GameManager

    private Font m_Fuente;
    private Text m_TextoReloj;
    private Text m_TextoMensaje;
    private Text m_TextoJugador1;
    private Text m_TextoJugador2;
    private GameObject m_PanelFinal;
    private Text m_TextoFinalTitulo;
    private Text m_TextoFinalDatos;

    private void Awake()
    {
        m_Fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Construir();
    }

    private void Update()
    {
        // Atajo: R reinicia cuando el panel final está visible
        if (m_PanelFinal != null && m_PanelFinal.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            if (m_AlReiniciar != null) m_AlReiniciar();
        }
    }

    private void Construir()
    {
        // El botón del panel final necesita un EventSystem
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasGO = new GameObject("CanvasHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Escala con la pantalla para que se vea bien en cualquier resolución
        CanvasScaler escala = canvasGO.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = 0.5f;

        Transform raiz = canvasGO.transform;

        // RF-01: nombre y matrícula, esquina superior derecha
        Text identidad = CrearTexto("NombreMatricula", raiz, 34, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -20f), new Vector2(800f, 50f));
        identidad.text = m_NombreMatricula;

        // Marcadores de cada jugador
        m_TextoJugador1 = CrearTexto("Jugador1", raiz, 30, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(700f, 140f));
        m_TextoJugador2 = CrearTexto("Jugador2", raiz, 30, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -75f), new Vector2(700f, 140f));

        // Reloj, arriba al centro
        m_TextoReloj = CrearTexto("Reloj", raiz, 64, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(300f, 90f));

        // Mensajes (RONDA 1, quién ganó la ronda...), al centro
        m_TextoMensaje = CrearTexto("Mensaje", raiz, 60, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 500f));

        ConstruirPanelFinal(raiz);
    }

    private void ConstruirPanelFinal(Transform raiz)
    {
        m_PanelFinal = new GameObject("PanelFinal", typeof(RectTransform), typeof(Image));
        m_PanelFinal.transform.SetParent(raiz, false);

        RectTransform rt = (RectTransform)m_PanelFinal.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        m_PanelFinal.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

        m_TextoFinalTitulo = CrearTexto("FinalTitulo", m_PanelFinal.transform, 80, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1700f, 300f));
        m_TextoFinalDatos = CrearTexto("FinalDatos", m_PanelFinal.transform, 40, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1500f, 300f));

        // Botón para reiniciar
        GameObject boton = new GameObject("BotonReiniciar", typeof(RectTransform), typeof(Image), typeof(Button));
        boton.transform.SetParent(m_PanelFinal.transform, false);
        RectTransform rtb = (RectTransform)boton.transform;
        rtb.anchorMin = rtb.anchorMax = new Vector2(0.5f, 0.5f);
        rtb.pivot = new Vector2(0.5f, 0.5f);
        rtb.anchoredPosition = new Vector2(0f, -290f);
        rtb.sizeDelta = new Vector2(520f, 110f);
        boton.GetComponent<Image>().color = new Color(0.16f, 0.39f, 0.70f, 1f);
        boton.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (m_AlReiniciar != null) m_AlReiniciar();
        });

        Text etiqueta = CrearTexto("Etiqueta", boton.transform, 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform rte = (RectTransform)etiqueta.transform;
        rte.anchorMin = Vector2.zero;
        rte.anchorMax = Vector2.one;
        rte.offsetMin = Vector2.zero;
        rte.offsetMax = Vector2.zero;
        etiqueta.text = "REINICIAR (R)";

        m_PanelFinal.SetActive(false);
    }

    // Crea un texto con contorno para que se lea sobre el fondo del mapa
    private Text CrearTexto(string nombre, Transform padre, int tamano, TextAnchor alineacion,
                            Vector2 ancla, Vector2 pivote, Vector2 posicion, Vector2 dimension)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = dimension;

        Text t = go.GetComponent<Text>();
        t.font = m_Fuente;
        t.fontSize = tamano;
        t.fontStyle = FontStyle.Bold;
        t.alignment = alineacion;
        t.color = Color.white;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        Outline contorno = go.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contorno.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    // ---- Lo que llama el GameManager ----

    public void ActualizarReloj(float segundos)
    {
        int t = Mathf.CeilToInt(Mathf.Max(0f, segundos));
        m_TextoReloj.text = (t / 60) + ":" + (t % 60).ToString("00");
        m_TextoReloj.color = t <= 30 ? new Color(1f, 0.35f, 0.3f) : Color.white; // rojo en los últimos 30 s
    }

    public void ActualizarJugador(int numero, string nombreColoreado, int perdidos, int maximo, int puntos)
    {
        Text destino = numero == 1 ? m_TextoJugador1 : m_TextoJugador2;
        destino.text = nombreColoreado
            + "\nIntentos perdidos: " + perdidos + " / " + maximo
            + "\nPuntos: " + puntos;
    }

    public void MostrarMensaje(string texto)
    {
        m_TextoMensaje.text = texto;
    }

    public void MostrarFinal(string titulo, string datos)
    {
        m_TextoFinalTitulo.text = titulo;
        m_TextoFinalDatos.text = datos;
        m_PanelFinal.SetActive(true);
    }

    public void OcultarFinal()
    {
        if (m_PanelFinal != null) m_PanelFinal.SetActive(false);
    }
}
