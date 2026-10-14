using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Herramientas del menú "Tanques" para no hacer a mano los pasos repetitivos.
public static class ConfiguracionTanques
{
    // ---------- 1. Capas y ejes de entrada ----------

    [MenuItem("Tanques/1. Configurar capas y ejes de entrada")]
    public static void ConfigurarCapasYEjes()
    {
        AgregarCapa("Players");
        AgregarCapa("Enemigos");

        SerializedObject inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
        SerializedProperty ejes = inputManager.FindProperty("m_Axes");

        // Jugador 1: WASD y barra espaciadora
        ConfigurarEje(ejes, "Horizontal1", "a", "d");
        ConfigurarEje(ejes, "Vertical1", "s", "w");
        ConfigurarBoton(ejes, "Fire1", "space");

        // Jugador 2: flechas y Enter
        ConfigurarEje(ejes, "Horizontal2", "left", "right");
        ConfigurarEje(ejes, "Vertical2", "down", "up");
        ConfigurarBoton(ejes, "Fire2", "return");

        inputManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("Tanques: capas Players y Enemigos listas; ejes Horizontal1/2, Vertical1/2, Fire1/2 configurados.");
    }

    private static void AgregarCapa(string nombre)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty capas = tagManager.FindProperty("layers");

        for (int i = 0; i < capas.arraySize; i++)
        {
            if (capas.GetArrayElementAtIndex(i).stringValue == nombre) return; // ya existe
        }

        // Las capas 0 a 7 son del sistema; las del usuario empiezan en la 8
        for (int i = 8; i < capas.arraySize; i++)
        {
            SerializedProperty capa = capas.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(capa.stringValue))
            {
                capa.stringValue = nombre;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
        Debug.LogError("Tanques: no hay capas libres para '" + nombre + "'.");
    }

    private static SerializedProperty BuscarEje(SerializedProperty ejes, string nombre)
    {
        for (int i = 0; i < ejes.arraySize; i++)
        {
            SerializedProperty e = ejes.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("m_Name").stringValue == nombre) return e;
        }
        return null;
    }

    private static SerializedProperty ObtenerOCrear(SerializedProperty ejes, string nombre)
    {
        SerializedProperty e = BuscarEje(ejes, nombre);
        if (e != null) return e;

        ejes.arraySize++;
        return ejes.GetArrayElementAtIndex(ejes.arraySize - 1);
    }

    // Eje de dos teclas (por ejemplo A y D)
    private static void ConfigurarEje(SerializedProperty ejes, string nombre, string negativo, string positivo)
    {
        SerializedProperty e = ObtenerOCrear(ejes, nombre);
        e.FindPropertyRelative("m_Name").stringValue = nombre;
        e.FindPropertyRelative("descriptiveName").stringValue = "";
        e.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
        e.FindPropertyRelative("negativeButton").stringValue = negativo;
        e.FindPropertyRelative("positiveButton").stringValue = positivo;
        e.FindPropertyRelative("altNegativeButton").stringValue = "";
        e.FindPropertyRelative("altPositiveButton").stringValue = "";
        e.FindPropertyRelative("gravity").floatValue = 3f;
        e.FindPropertyRelative("dead").floatValue = 0.001f;
        e.FindPropertyRelative("sensitivity").floatValue = 3f;
        e.FindPropertyRelative("snap").boolValue = true;
        e.FindPropertyRelative("invert").boolValue = false;
        e.FindPropertyRelative("type").intValue = 0;   // tecla o botón del ratón
        e.FindPropertyRelative("axis").intValue = 0;
        e.FindPropertyRelative("joyNum").intValue = 0;
    }

    // Botón de una tecla (disparo). Cambia el Fire1/Fire2 que Unity trae por defecto.
    private static void ConfigurarBoton(SerializedProperty ejes, string nombre, string tecla)
    {
        SerializedProperty e = ObtenerOCrear(ejes, nombre);
        e.FindPropertyRelative("m_Name").stringValue = nombre;
        e.FindPropertyRelative("descriptiveName").stringValue = "";
        e.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
        e.FindPropertyRelative("negativeButton").stringValue = "";
        e.FindPropertyRelative("positiveButton").stringValue = tecla;
        e.FindPropertyRelative("altNegativeButton").stringValue = "";
        e.FindPropertyRelative("altPositiveButton").stringValue = "";
        e.FindPropertyRelative("gravity").floatValue = 1000f;
        e.FindPropertyRelative("dead").floatValue = 0.001f;
        e.FindPropertyRelative("sensitivity").floatValue = 1000f;
        e.FindPropertyRelative("snap").boolValue = false;
        e.FindPropertyRelative("invert").boolValue = false;
        e.FindPropertyRelative("type").intValue = 0;
        e.FindPropertyRelative("axis").intValue = 0;
        e.FindPropertyRelative("joyNum").intValue = 0;
    }

    // ---------- 2. SpawnPoints y puntos de enemigos ----------

    private static void CrearPuntosProfesor()
    {
        // Valores del PDF del profesor
        CrearPunto("SpawnPoint1", new Vector3(-3f, 0f, 30f), Quaternion.Euler(0f, 180f, 0f), "sv_label_1"); // azul
        CrearPunto("SpawnPoint2", new Vector3(13f, 0f, -5f), Quaternion.identity, "sv_label_6");            // rojo

        // Tres enemigos entre los tanques (ajusta la posición mirando la escena)
        CrearPuntoEnemigo("PuntoEnemigo1", new Vector3(5f, 0f, 12f), Quaternion.identity);
        CrearPuntoEnemigo("PuntoEnemigo2", new Vector3(0f, 0f, 20f), Quaternion.Euler(0f, 90f, 0f));
        CrearPuntoEnemigo("PuntoEnemigo3", new Vector3(10f, 0f, 5f), Quaternion.Euler(0f, 45f, 0f));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Tanques: puntos creados. Revisa en la escena que los PuntoEnemigo no queden dentro de rocas o edificios.");
    }

    public static GameObject CrearPunto(string nombre, Vector3 posicion, Quaternion rotacion, string icono)
    {
        GameObject existente = GameObject.Find(nombre);
        if (existente != null) return existente;

        GameObject go = new GameObject(nombre);
        go.transform.position = posicion;
        go.transform.rotation = rotacion;
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);

        try
        {
            Texture2D tex = EditorGUIUtility.IconContent(icono).image as Texture2D;
            if (tex != null) EditorGUIUtility.SetIconForObject(go, tex);
        }
        catch { /* el icono es solo decorativo */ }
        return go;
    }

    public static void CrearPuntoEnemigo(string nombre, Vector3 posicion, Quaternion rotacion)
    {
        GameObject go = CrearPunto(nombre, posicion, rotacion, "sv_label_5"); // naranja
        if (go.GetComponent<PuntoEnemigo>() == null) go.AddComponent<PuntoEnemigo>();
    }

    // ---------- 3. Prefab del enemigo ----------

    [MenuItem("Tanques/3. Crear prefab Enemigo")]
    public static void CrearPrefabEnemigo()
    {
        AsegurarCarpeta("Assets/Materials");
        AsegurarCarpeta("Assets/Prefabs");

        string rutaMaterial = "Assets/Materials/Enemigo.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = new Color(0.55f, 0.12f, 0.62f); // morado
            AssetDatabase.CreateAsset(material, rutaMaterial);
        }

        GameObject enemigo = EnemigoPatrulla.Construir(material);
        int capa = LayerMask.NameToLayer("Enemigos");
        if (capa >= 0)
        {
            foreach (Transform t in enemigo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = capa;
        }

        PrefabUtility.SaveAsPrefabAsset(enemigo, "Assets/Prefabs/Enemigo.prefab");
        Object.DestroyImmediate(enemigo);
        AssetDatabase.SaveAssets();
        Debug.Log("Tanques: prefab creado en Assets/Prefabs/Enemigo.prefab");
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (!AssetDatabase.IsValidFolder(ruta))
        {
            AssetDatabase.CreateFolder("Assets", ruta.Substring("Assets/".Length));
        }
    }
}
