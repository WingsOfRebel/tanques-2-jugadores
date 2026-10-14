using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú Tanques > 4: arma el juego completo con los assets del paquete "Tanks!":
// prefabs de la bomba y del tanque, escena Principal (nivel, cámara, luz, puntos,
// enemigos, GameManager y música). Deja un registro en Tanques_construccion.txt.
public static class ConstruirJuego
{
    private const string Paquete = "Assets/_Tanks";
    private static StringBuilder m_Log;

    [MenuItem("Tanques/4. Construir escena y prefabs")]
    public static void Construir()
    {
        m_Log = new StringBuilder();
        bool ok = false;
        try
        {
            if (LayerMask.NameToLayer("Players") < 0 || LayerMask.NameToLayer("Enemigos") < 0)
                throw new System.Exception("Faltan las capas Players y Enemigos. Ejecuta primero Tanques > 1.");

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AsegurarCarpeta("Assets/Prefabs");
            AsegurarCarpeta("Assets/Scenes");

            GameObject bomba = CrearPrefabBomba();
            ConfiguracionTanques.CrearPrefabEnemigo();
            GameObject enemigo = Cargar<GameObject>("Assets/Prefabs/Enemigo.prefab");
            GameObject tanque = CrearPrefabTanque(bomba);
            CrearEscena(tanque, enemigo);
            ok = true;
        }
        catch (System.Exception e)
        {
            Log("ERROR: " + e);
        }
        finally
        {
            string ruta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tanques_construccion.txt"));
            File.WriteAllText(ruta, m_Log.ToString());
        }

        EditorUtility.DisplayDialog("Tanques", ok
            ? "Listo. Escena Principal creada. Detalles en Tanques_construccion.txt"
            : "Hubo un error. Revisa Tanques_construccion.txt (o dime y lo reviso yo).", "OK");
    }

    private static void Log(string texto)
    {
        m_Log.AppendLine(texto);
        Debug.Log("Tanques: " + texto);
    }

    private static T Cargar<T>(string ruta) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (asset == null) throw new System.Exception("No se encontró " + ruta);
        return asset;
    }

    private static string BuscarPrefab(string nombre)
    {
        string[] guids = AssetDatabase.FindAssets(nombre, new[] { Paquete });
        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (Path.GetFileNameWithoutExtension(p) == nombre && p.EndsWith(".prefab")) return p;
        }
        return null;
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (!AssetDatabase.IsValidFolder(ruta))
            AssetDatabase.CreateFolder("Assets", ruta.Substring("Assets/".Length));
    }

    private static void QuitarScriptsFaltantes(GameObject raiz)
    {
        Transform[] todos = raiz.GetComponentsInChildren<Transform>(true);
        int quitados = 0;
        for (int i = 0; i < todos.Length; i++)
        {
            try { quitados += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(todos[i].gameObject); }
            catch (System.Exception e) { Log("Aviso al limpiar " + todos[i].name + ": " + e.Message); }
        }
        Log("Scripts faltantes eliminados en " + raiz.name + ": " + quitados);
    }

    // ---------------- Bomba ----------------

    private static GameObject CrearPrefabBomba()
    {
        string origen = Paquete + "/Prefabs/Explosives/CompleteShell.prefab";
        string destino = "Assets/Prefabs/Shell.prefab";
        AssetDatabase.DeleteAsset(destino);
        if (!AssetDatabase.CopyAsset(origen, destino)) throw new System.Exception("No se pudo copiar " + origen);

        GameObject raiz = PrefabUtility.LoadPrefabContents(destino);
        try
        {
            QuitarScriptsFaltantes(raiz);

            ShellExplosion se = raiz.GetComponent<ShellExplosion>();
            if (se == null) se = raiz.AddComponent<ShellExplosion>();

            Transform exp = raiz.transform.Find("CompleteShellExplosion");
            if (exp == null) throw new System.Exception("La bomba no tiene el hijo CompleteShellExplosion");

            se.m_ExplosionParticles = exp.GetComponent<ParticleSystem>();
            se.m_ExplosionAudio = exp.GetComponent<AudioSource>();
            se.m_TankMask = LayerMask.GetMask("Players");

            // Las partículas y el sonido solo deben activarse al explotar
            ParticleSystem[] sistemas = raiz.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < sistemas.Length; i++)
            {
                ParticleSystem.MainModule main = sistemas[i].main;
                main.playOnAwake = false;
            }
            se.m_ExplosionAudio.playOnAwake = false;

            PrefabUtility.SaveAsPrefabAsset(raiz, destino);
            Log("Prefab de la bomba listo: " + destino);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(destino);
    }

    // ---------------- Tanque ----------------

    private static void Estirar(Transform t)
    {
        RectTransform rt = t as RectTransform;
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CrearPrefabTanque(GameObject bomba)
    {
        AudioClip idle = Cargar<AudioClip>(Paquete + "/Audio/SFX/EngineIdle.aif");
        AudioClip driving = Cargar<AudioClip>(Paquete + "/Audio/SFX/EngineDriving.aif");
        AudioClip cargando = Cargar<AudioClip>(Paquete + "/Audio/SFX/ShotCharging.wav");
        AudioClip disparo = Cargar<AudioClip>(Paquete + "/Audio/SFX/ShotFiring.wav");

        GameObject raiz = new GameObject("Tank");
        raiz.layer = LayerMask.NameToLayer("Players");

        // Física (como en el PDF): congelar posición Y y rotación X y Z
        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        BoxCollider caja = raiz.AddComponent<BoxCollider>();
        caja.center = new Vector3(0f, 0.85f, 0f);
        caja.size = new Vector3(1.5f, 1.7f, 1.6f);

        // Dos fuentes de audio: motor y disparo
        AudioSource motor = raiz.AddComponent<AudioSource>();
        motor.clip = idle;
        motor.loop = true;
        motor.playOnAwake = true;
        AudioSource audioDisparo = raiz.AddComponent<AudioSource>();
        audioDisparo.playOnAwake = false;

        // Modelo
        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(Cargar<GameObject>(Paquete + "/Art/Models/Tanks/Tank_Original_Model.fbx"));
        modelo.name = "TankRenderers";
        modelo.transform.SetParent(raiz.transform, false);

        // Polvo de las orugas
        GameObject polvoPrefab = Cargar<GameObject>(Paquete + "/Prefabs/DustTrails/DustTrail_Desert.prefab");
        GameObject izq = (GameObject)PrefabUtility.InstantiatePrefab(polvoPrefab);
        izq.name = "LeftDustTrail";
        izq.transform.SetParent(raiz.transform, false);
        izq.transform.localPosition = new Vector3(-0.5f, 0.1f, -0.75f);
        GameObject der = (GameObject)PrefabUtility.InstantiatePrefab(polvoPrefab);
        der.name = "RightDustTrail";
        der.transform.SetParent(raiz.transform, false);
        der.transform.localPosition = new Vector3(0.5f, 0.1f, -0.75f);

        // Punto de salida de la bomba
        GameObject fire = new GameObject("FireTransform");
        fire.transform.SetParent(raiz.transform, false);
        fire.transform.localPosition = new Vector3(0f, 1.7f, 1.35f);
        fire.transform.localEulerAngles = new Vector3(358f, 0f, 0f);

        // Canvas en el mundo con las barras de salud y de disparo
        GameObject canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(raiz.transform, false);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        CanvasScaler escala = canvasGO.GetComponent<CanvasScaler>();
        escala.dynamicPixelsPerUnit = 1f;
        escala.referencePixelsPerUnit = 1f;
        RectTransform rtc = canvasGO.GetComponent<RectTransform>();
        rtc.sizeDelta = new Vector2(3.5f, 3.5f);
        rtc.localPosition = new Vector3(0f, 0.1f, 0f);
        rtc.localEulerAngles = new Vector3(90f, 0f, 0f);
        rtc.localScale = Vector3.one;

        // Barra de salud (prefab del paquete)
        GameObject health = (GameObject)PrefabUtility.InstantiatePrefab(Cargar<GameObject>(Paquete + "/Prefabs/UI/HealthSlider.prefab"));
        health.name = "HealthSlider";
        health.transform.SetParent(canvasGO.transform, false);
        Estirar(health.transform);
        Estirar(health.transform.Find("Background"));
        Estirar(health.transform.Find("Fill Area"));
        Estirar(health.transform.Find("Fill Area/Fill"));

        Slider sliderSalud = health.GetComponent<Slider>();
        sliderSalud.interactable = false;
        sliderSalud.transition = Selectable.Transition.None;
        Image fondo = health.transform.Find("Background").GetComponent<Image>();
        fondo.color = new Color(1f, 1f, 1f, 80f / 255f);
        Image relleno = health.transform.Find("Fill Area/Fill").GetComponent<Image>();

        // Barra de fuerza del disparo
        Sprite flecha = AssetDatabase.LoadAssetAtPath<Sprite>(Paquete + "/Art/Sprites/Aim Arrow.png");
        if (flecha == null) Log("Aviso: no se pudo cargar el sprite Aim Arrow como Sprite");

        GameObject aim = new GameObject("AimSlider", typeof(RectTransform), typeof(Slider));
        aim.transform.SetParent(canvasGO.transform, false);
        RectTransform ra = aim.GetComponent<RectTransform>();
        ra.anchorMin = Vector2.zero;
        ra.anchorMax = Vector2.one;
        ra.offsetMin = new Vector2(1f, 3f);
        ra.offsetMax = new Vector2(-1f, 9f);
        Vector3 pos3 = ra.anchoredPosition3D;
        pos3.z = -1f;
        ra.anchoredPosition3D = pos3;

        GameObject area = new GameObject("Fill Area", typeof(RectTransform));
        area.transform.SetParent(aim.transform, false);
        Estirar(area.transform);
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(area.transform, false);
        Estirar(fill.transform);
        Image imgFlecha = fill.GetComponent<Image>();
        imgFlecha.sprite = flecha;
        imgFlecha.type = Image.Type.Sliced;

        Slider sliderAim = aim.GetComponent<Slider>();
        sliderAim.fillRect = fill.GetComponent<RectTransform>();
        sliderAim.handleRect = null;
        sliderAim.direction = Slider.Direction.BottomToTop;
        sliderAim.minValue = 15f;
        sliderAim.maxValue = 30f;
        sliderAim.value = 15f;
        sliderAim.interactable = false;
        sliderAim.transition = Selectable.Transition.None;

        // La barra de salud mantiene su orientación aunque el tanque gire
        health.AddComponent<UIDirectionControl>();

        // Scripts del tanque
        TankMovement mov = raiz.AddComponent<TankMovement>();
        mov.m_MovementAudio = motor;
        mov.m_EngineIdling = idle;
        mov.m_EngineDriving = driving;

        TankShooting tir = raiz.AddComponent<TankShooting>();
        tir.m_Shell = bomba.GetComponent<Rigidbody>();
        tir.m_FireTransform = fire.transform;
        tir.m_AimSlider = sliderAim;
        tir.m_ShootingAudio = audioDisparo;
        tir.m_ChargingClip = cargando;
        tir.m_FireClip = disparo;

        TankHealth salud = raiz.AddComponent<TankHealth>();
        salud.m_Slider = sliderSalud;
        salud.m_FillImage = relleno;
        salud.m_ExplosionPrefab = Cargar<GameObject>(Paquete + "/Prefabs/Explosives/TankExplosion.prefab");

        string destino = "Assets/Prefabs/Tank.prefab";
        PrefabUtility.SaveAsPrefabAsset(raiz, destino);
        Object.DestroyImmediate(raiz);
        Log("Prefab del tanque listo: " + destino);
        return AssetDatabase.LoadAssetAtPath<GameObject>(destino);
    }

    // ---------------- Escena ----------------

    private static void CrearEscena(GameObject tanque, GameObject enemigo)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Nivel
        GameObject nivel = (GameObject)PrefabUtility.InstantiatePrefab(Cargar<GameObject>(Paquete + "/Prefabs/Levels/LevelDesert.prefab"));
        nivel.name = "LevelDesert";

        // Luz del paquete (si existe) en lugar de la luz por defecto
        string rutaLuz = BuscarPrefab("DirectionalLightSand");
        if (rutaLuz != null)
        {
            Light[] luces = Object.FindObjectsByType<Light>();
            for (int i = 0; i < luces.Length; i++)
            {
                if (luces[i].type == LightType.Directional && luces[i].transform.parent == null)
                    Object.DestroyImmediate(luces[i].gameObject);
            }
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(rutaLuz));
            Log("Luz del paquete: " + rutaLuz);
        }
        else
        {
            Log("No se encontró DirectionalLightSand: se deja la luz por defecto");
        }

        // Iluminación ambiental (valores del PDF)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color32(72, 62, 113, 255);

        // CameraRig + cámara ortográfica (valores del PDF)
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGO.tag = "MainCamera";
            cam = camGO.GetComponent<Camera>();
        }
        GameObject rig = new GameObject("CameraRig");
        rig.transform.position = Vector3.zero;
        rig.transform.rotation = Quaternion.Euler(40f, 60f, 0f);
        cam.transform.SetParent(rig.transform, false);
        cam.transform.localPosition = new Vector3(0f, 0f, -65f);
        cam.transform.localRotation = Quaternion.identity;
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(80, 60, 50, 255);
        CameraControl control = rig.AddComponent<CameraControl>();

        // Puntos de aparición y de enemigos, en lugares libres del nivel
        Physics.SyncTransforms();
        Vector3 ejeX = Quaternion.Euler(0f, 60f, 0f) * Vector3.right;    // horizontal de la pantalla
        Vector3 ejeY = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward;  // vertical de la pantalla

        Vector3 pos1 = BuscarLibre(-16f * ejeX, Vector3.zero, 0f);
        Vector3 pos2 = BuscarLibre(16f * ejeX, Vector3.zero, 0f);
        GameObject sp1 = ConfiguracionTanques.CrearPunto("SpawnPoint1", pos1, Quaternion.LookRotation(Plano(pos2 - pos1)), "sv_label_1");
        GameObject sp2 = ConfiguracionTanques.CrearPunto("SpawnPoint2", pos2, Quaternion.LookRotation(Plano(pos1 - pos2)), "sv_label_6");
        Log("SpawnPoint1 " + pos1 + "  SpawnPoint2 " + pos2);

        float[] desplazamientos = { -6f, 0f, 6f };
        for (int i = 0; i < desplazamientos.Length; i++)
        {
            Vector3 p = BuscarLibre(desplazamientos[i] * ejeX, ejeY, 4f);
            GameObject go = ConfiguracionTanques.CrearPunto("PuntoEnemigo" + (i + 1), p, Quaternion.FromToRotation(Vector3.right, ejeY), "sv_label_5");
            if (go.GetComponent<PuntoEnemigo>() == null) go.AddComponent<PuntoEnemigo>();
            Log("PuntoEnemigo" + (i + 1) + " " + p);
        }

        // GameManager
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm = gmGO.AddComponent<GameManager>();
        gm.m_CameraControl = control;
        gm.m_TankPrefab = tanque;
        gm.m_EnemigoPrefab = enemigo;
        gm.m_Tanks = new TankManager[2];
        gm.m_Tanks[0] = new TankManager { m_PlayerColor = new Color32(42, 100, 178, 255), m_SpawnPoint = sp1.transform };
        gm.m_Tanks[1] = new TankManager { m_PlayerColor = new Color32(229, 46, 40, 255), m_SpawnPoint = sp2.transform };

        // Música (la grabación se asigna después)
        GameObject musica = new GameObject("MusicaFondo");
        musica.AddComponent<MusicaFondo>();

        EditorSceneManager.SaveScene(escena, "Assets/Scenes/Principal.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Principal.unity", true) };
        Selection.activeGameObject = gmGO;
        Log("Escena guardada: Assets/Scenes/Principal.unity (añadida a la lista de escenas del build)");
    }

    private static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.01f ? Vector3.forward : v.normalized;
    }

    // Busca, en espiral alrededor del objetivo, un sitio sin colliders (sin contar el suelo).
    // Si dir != 0 exige que todo el recorrido +/- distancia esté libre.
    private static Vector3 BuscarLibre(Vector3 objetivo, Vector3 dir, float distancia)
    {
        for (float radio = 0f; radio <= 30f; radio += 2f)
        {
            int pasos = radio == 0f ? 1 : 12;
            for (int a = 0; a < pasos; a++)
            {
                float ang = a * Mathf.PI * 2f / pasos;
                Vector3 c = objetivo + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radio;
                c.y = 0f;
                if (distancia <= 0f)
                {
                    if (EspacioLibre(c, 1.6f)) return c;
                }
                else
                {
                    bool libre = true;
                    for (float t = -distancia; t <= distancia && libre; t += 1.5f)
                        libre = EspacioLibre(c + dir * t, 1.8f);
                    if (libre) return c;
                }
            }
        }
        Log("Aviso: no se encontró un sitio libre cerca de " + objetivo + "; se usa tal cual (revisa en la escena)");
        return objetivo;
    }

    // Caja un poco por encima del suelo: si toca algo, hay un obstáculo
    private static bool EspacioLibre(Vector3 c, float mitad)
    {
        Collider[] golpes = Physics.OverlapBox(c + Vector3.up * 1.4f, new Vector3(mitad, 0.6f, mitad), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        return golpes.Length == 0;
    }
}
