using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Escribe Tanques_reporte.txt (en la raíz del proyecto) con la estructura de los
// prefabs y modelos del paquete, para poder armar el juego con los nombres reales.
public static class InspeccionPaquete
{
    [MenuItem("Tanques/0. Inspeccionar paquete (crea Tanques_reporte.txt)")]
    public static void Inspeccionar()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Unity " + Application.unityVersion);
        sb.AppendLine("Pipeline: " + (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
            ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.name : "NINGUNO"));
        sb.AppendLine();

        string[] nombres =
        {
            "Demo_Tank", "CompleteShell", "CompleteShellExplosion", "TankExplosion", "HealthSlider",
            "DustTrail_Desert", "Tank_Original_Model", "Shell", "LevelDesert", "LevelDesert_Empty",
            "PowerUpSpawner"
        };

        for (int i = 0; i < nombres.Length; i++)
        {
            string ruta = BuscarRuta(nombres[i]);
            sb.AppendLine("==================== " + nombres[i] + "  ->  " + (ruta ?? "NO ENCONTRADO"));
            if (ruta == null) continue;

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (asset == null) { sb.AppendLine("(no es un GameObject)"); continue; }

            bool esNivel = nombres[i].StartsWith("Level");
            Arbol(sb, asset.transform, 0, esNivel ? 1 : 6);
            Medidas(sb, asset);
            sb.AppendLine();
        }

        // Sonidos y sprites que se usarán
        sb.AppendLine("==================== Archivos clave");
        string[] clave = { "EngineIdle", "EngineDriving", "ShotCharging", "ShotFiring", "ShellExplosion", "TankExplosion",
                           "Health Wheel", "Aim Arrow", "TankColor", "Music_Classic" };
        for (int i = 0; i < clave.Length; i++)
        {
            string[] guids = AssetDatabase.FindAssets(clave[i], new[] { "Assets/_Tanks" });
            for (int g = 0; g < guids.Length; g++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (Path.GetFileNameWithoutExtension(p) == clave[i])
                    sb.AppendLine(clave[i] + " -> " + p + "  (" + AssetDatabase.GetMainAssetTypeAtPath(p).Name + ")");
            }
        }

        string salida = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tanques_reporte.txt"));
        File.WriteAllText(salida, sb.ToString());
        Debug.Log("Tanques: reporte escrito en " + salida);
        EditorUtility.DisplayDialog("Tanques", "Reporte creado: Tanques_reporte.txt (en la carpeta del proyecto).", "OK");
    }

    // Busca un asset por nombre exacto dentro de Assets/_Tanks (prefab o modelo)
    private static string BuscarRuta(string nombre)
    {
        string[] guids = AssetDatabase.FindAssets(nombre, new[] { "Assets/_Tanks" });
        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            string ext = Path.GetExtension(p).ToLower();
            if (Path.GetFileNameWithoutExtension(p) == nombre && (ext == ".prefab" || ext == ".fbx"))
                return p;
        }
        return null;
    }

    private static void Arbol(StringBuilder sb, Transform t, int nivel, int maxNivel)
    {
        string ind = new string(' ', nivel * 2);
        sb.Append(ind + t.name + "  pos" + t.localPosition.ToString("F2") + " rot" + t.localEulerAngles.ToString("F0")
                  + " esc" + t.localScale.ToString("F2") + " capa" + t.gameObject.layer + " :: ");

        Component[] comps = t.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) { sb.Append("[SCRIPT FALTANTE]; "); continue; }
            sb.Append(comps[i].GetType().Name + Detalle(comps[i]) + "; ");
        }
        sb.AppendLine();

        if (nivel < maxNivel)
        {
            foreach (Transform hijo in t) Arbol(sb, hijo, nivel + 1, maxNivel);
        }
        else if (t.childCount > 0)
        {
            sb.AppendLine(ind + "  ... " + t.childCount + " hijos (no se listan)");
        }
    }

    private static string Detalle(Component c)
    {
        if (c is BoxCollider bc) return "{c" + bc.center.ToString("F2") + " s" + bc.size.ToString("F2") + " trig" + bc.isTrigger + "}";
        if (c is CapsuleCollider cc) return "{c" + cc.center.ToString("F2") + " r" + cc.radius.ToString("F2") + " h" + cc.height.ToString("F2") + " dir" + cc.direction + " trig" + cc.isTrigger + "}";
        if (c is SphereCollider sc) return "{r" + sc.radius.ToString("F2") + " trig" + sc.isTrigger + "}";
        if (c is MeshCollider mc) return "{convex" + mc.convex + "}";
        if (c is Rigidbody rb) return "{masa" + rb.mass + " kin" + rb.isKinematic + " grav" + rb.useGravity + " cons" + rb.constraints + "}";
        if (c is AudioSource a) return "{clip=" + (a.clip != null ? a.clip.name : "ninguno") + " awake" + a.playOnAwake + " loop" + a.loop + "}";
        if (c is Light l) return "{" + l.type + "}";
        if (c is Canvas cv) return "{" + cv.renderMode + "}";
        if (c is Slider s) return "{min" + s.minValue + " max" + s.maxValue + " dir" + s.direction + "}";
        if (c is Image im) return "{sprite=" + (im.sprite != null ? im.sprite.name : "ninguno") + " tipo" + im.type + "}";
        if (c is RectTransform rt) return "{tam" + rt.sizeDelta.ToString("F1") + " pos" + rt.anchoredPosition3D.ToString("F1") + "}";
        if (c is ParticleSystem ps) return "{dur" + ps.main.duration + " loop" + ps.main.loop + "}";
        if (c is MeshRenderer mr)
        {
            string m = "";
            for (int i = 0; i < mr.sharedMaterials.Length; i++) m += (mr.sharedMaterials[i] != null ? mr.sharedMaterials[i].name : "null") + ",";
            return "{mat=" + m + "}";
        }
        if (c is SkinnedMeshRenderer smr) return "{skinned}";
        return "";
    }

    // Medidas totales del asset instanciado en una escena temporal
    private static void Medidas(StringBuilder sb, GameObject asset)
    {
        UnityEngine.SceneManagement.Scene temp = EditorSceneManager.NewPreviewScene();
        GameObject inst = Object.Instantiate(asset);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(inst, temp);

        Renderer[] rends = inst.GetComponentsInChildren<Renderer>(true);
        Collider[] cols = inst.GetComponentsInChildren<Collider>(true);
        Bounds b = new Bounds(inst.transform.position, Vector3.zero);
        bool primero = true;
        for (int i = 0; i < rends.Length; i++)
        {
            if (primero) { b = rends[i].bounds; primero = false; } else b.Encapsulate(rends[i].bounds);
        }
        sb.AppendLine("  >> renderers=" + rends.Length + " colliders=" + cols.Length
                      + " centro" + b.center.ToString("F1") + " tamaño" + b.size.ToString("F1"));

        Object.DestroyImmediate(inst);
        EditorSceneManager.ClosePreviewScene(temp);
    }
}
