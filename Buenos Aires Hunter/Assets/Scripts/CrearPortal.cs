using UnityEngine;

/// <summary>
/// Portal circular/ovalado hecho con Particle Systems + un disco opaco interior.
/// Uso: crea un GameObject vacío, agrégale este script.
/// El portal queda en el plano XY. Se ve también en el editor sin Play.
/// </summary>
[ExecuteAlways]
public class CrearPortal : MonoBehaviour
{
    [Header("Forma")]
    public float radius = 1.5f;
    [Tooltip("1,1 = círculo. Ej: 1,1.6 = óvalo vertical")]
    public Vector2 ovalScale = new Vector2(1f, 1.6f);
    public float ringThickness = 0.2f;

    [Header("Rotación (radianes/seg, positivo = antihorario)")]
    public float orbitalSpeed = 0.5f;

    [Header("Colores")]
    public Color colorA = new Color(0.2f, 0.8f, 1f, 1f);
    public Color colorB = new Color(0.65f, 0.25f, 1f, 1f);

    [Header("Partículas del anillo")]
    [Tooltip("Límite máximo de partículas simultáneas del anillo")]
    [Min(1)] public int ringMaxParticles = 1500;
    [Tooltip("Cantidad de partículas nuevas emitidas por segundo")]
    [Min(0f)] public float ringEmissionRate = 300f;
    [Tooltip("Vida mínima y máxima de cada partícula")]
    public Vector2 ringLifetime = new Vector2(1.5f, 2.5f);
    [Tooltip("Tamaño mínimo y máximo de cada partícula")]
    public Vector2 ringStartSize = new Vector2(0.12f, 0.25f);

    [Header("Estela del anillo")]
    [Tooltip("Largo de la estela como fracción de la vida de la partícula. 0 = sin estela")]
    [Range(0f, 1f)] public float trailLength = 0.35f;

    [Header("Interior opaco")]
    public bool opaqueFill = true;
    [Tooltip("Color del fondo del portal (oscuro queda mejor)")]
    public Color fillColor = new Color(0.1f, 0.02f, 0.02f, 1f);
    [Tooltip("Separación del disco respecto a las partículas. Si el disco se ve por delante del anillo, probá el valor en negativo")]
    public float fillOffsetZ = 0.05f;
    [Tooltip("Qué tan visible es la neblina que gira sobre el fondo")]
    [Range(0f, 1f)] public float mistAlpha = 0.35f;

    [Header("Partículas del interior (Mist)")]
    [Tooltip("Límite máximo de partículas de la neblina")]
    [Min(1)] public int mistMaxParticles = 150;
    [Tooltip("Cantidad de partículas nuevas emitidas por segundo")]
    [Min(0f)] public float mistEmissionRate = 25f;
    [Tooltip("Vida mínima y máxima de cada partícula")]
    public Vector2 mistLifetime = new Vector2(2.5f, 3.5f);
    [Tooltip("Tamaño mínimo y máximo de cada partícula")]
    public Vector2 mistStartSize = new Vector2(1.2f, 2.2f);
    [Tooltip("Velocidad de rotación de la neblina")]
    public float mistRotationSpeed = 0.3f;
    [Tooltip("Radio de emisión respecto al radio del portal")]
    [Min(0f)] public float mistRadiusMultiplier = 0.8f;

    [Header("Materiales (opcionales)")]
    [Tooltip("Material de partículas aditivo. Si está vacío se crea uno automático.")]
    public Material particleMaterial;
    [Tooltip("Material opaco para el interior. Si está vacío se crea uno Unlit con Fill Color.")]
    public Material fillMaterial;
    [Header("Audio")]
    public AudioClip spawnSound;
    [Range(0f, 1f)] public float spawnVolume = 1f;

    void PlaySpawnSound()
    {
        // PlayClipAtPoint crea un GameObject temporal llamado "One Shot Audio".
        // No debe ejecutarse mientras Unity está mostrando/actualizando el portal en el editor.
        if (!Application.isPlaying || spawnSound == null) return;

        AudioSource.PlayClipAtPoint(
            spawnSound,
            transform.position,
            spawnVolume
        );
    }

    GameObject root;
    Material autoMaterial;
    Texture2D autoTexture;
    Material autoFillMaterial;
    Mesh fillMesh;

    void OnEnable() { Build(); }
    void OnDisable() { Clear(); }

    [ContextMenu("Rebuild Portal")]
    public void Build()
    {
        Clear();

        root = new GameObject("Portal_Visual");
        root.hideFlags = HideFlags.DontSave;
        root.transform.SetParent(transform, false);
        root.transform.localScale = new Vector3(ovalScale.x, ovalScale.y, 1f);

        Material mat = particleMaterial != null ? particleMaterial : GetAutoMaterial();

        if (opaqueFill) CreateFill();
        CreateRing(mat);
        CreateSparks(mat);
        CreateMist(mat);
        PlaySpawnSound();
    }

    void Clear()
    {
        if (root != null) { SafeDestroy(root); root = null; }
        if (autoMaterial != null) { SafeDestroy(autoMaterial); autoMaterial = null; }
        if (autoTexture != null) { SafeDestroy(autoTexture); autoTexture = null; }
        if (autoFillMaterial != null) { SafeDestroy(autoFillMaterial); autoFillMaterial = null; }
        if (fillMesh != null) { SafeDestroy(fillMesh); fillMesh = null; }
    }

    static void SafeDestroy(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    // ---------- Disco opaco ----------

    void CreateFill()
    {
        var go = new GameObject("Portal_Fill");
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, fillOffsetZ);

        fillMesh = MakeDiscMesh(radius, 64);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = fillMesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = fillMaterial != null ? fillMaterial : GetAutoFillMaterial();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    // Disco de doble cara (se ve desde adelante y desde atrás)
    Mesh MakeDiscMesh(float r, int segments)
    {
        var mesh = new Mesh { name = "PortalDisc", hideFlags = HideFlags.DontSave };

        var verts = new Vector3[segments + 1];
        var uvs = new Vector2[segments + 1];
        verts[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float ang = (float)i / segments * Mathf.PI * 2f;
            float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            verts[i + 1] = new Vector3(c * r, s * r, 0f);
            uvs[i + 1] = new Vector2(0.5f + c * 0.5f, 0.5f + s * 0.5f);
        }

        var tris = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int cur = i + 1;
            int next = (i + 1) % segments + 1;
            int t = i * 6;
            // cara frontal
            tris[t + 0] = 0; tris[t + 1] = cur; tris[t + 2] = next;
            // cara trasera
            tris[t + 3] = 0; tris[t + 4] = next; tris[t + 5] = cur;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    Material GetAutoFillMaterial()
    {
        if (autoFillMaterial != null) return autoFillMaterial;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        autoFillMaterial = new Material(sh) { hideFlags = HideFlags.DontSave };
        if (autoFillMaterial.HasProperty("_BaseColor")) autoFillMaterial.SetColor("_BaseColor", fillColor);
        if (autoFillMaterial.HasProperty("_Color")) autoFillMaterial.SetColor("_Color", fillColor);
        return autoFillMaterial;
    }

    // ---------- Sistemas de partículas ----------

    void CreateRing(Material mat)
    {
        var ps = MakeSystem("Portal_Ring", mat, out var r);

        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Min(ringLifetime.x, ringLifetime.y),
            Mathf.Max(ringLifetime.x, ringLifetime.y));
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Min(ringStartSize.x, ringStartSize.y),
            Mathf.Max(ringStartSize.x, ringStartSize.y));
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.maxParticles = ringMaxParticles;

        var em = ps.emission;
        em.rateOverTime = ringEmissionRate;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Donut;
        sh.radius = radius;
        sh.donutRadius = ringThickness;
        sh.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = 0f; vel.y = 0f; vel.z = 0f;       // todos deben estar en el mismo modo
        vel.orbitalX = 0f; vel.orbitalY = 0f;
        vel.orbitalZ = orbitalSpeed;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(FadeGradient());

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

        // Estela: convierte cada "bolita" en un trazo
        var trails = ps.trails;
        trails.enabled = trailLength > 0f;
        if (trails.enabled)
        {
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(trailLength);
            trails.minVertexDistance = 0.03f;
            trails.worldSpace = false;
            trails.dieWithParticles = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(FadeGradient());
            r.trailMaterial = mat;
        }

        ps.Play();
    }

    void CreateSparks(Material mat)
    {
        var ps = MakeSystem("Portal_Sparks", mat, out var r);

        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, colorA);
        main.maxParticles = 500;

        var em = ps.emission;
        em.rateOverTime = 40f;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Donut;
        sh.radius = radius;
        sh.donutRadius = ringThickness * 1.5f;
        sh.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = 0f; vel.y = 0f; vel.z = 0f;
        vel.orbitalX = 0f; vel.orbitalY = 0f;
        vel.orbitalZ = -orbitalSpeed * 1.6f;      // giran al revés
        vel.radial = -0.35f;                      // son "succionadas" hacia el centro

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.8f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(FadeGradient());

        ps.Play();
    }

    void CreateMist(Material mat)
    {
        var ps = MakeSystem("Portal_Mist", mat, out var r);

        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Min(mistLifetime.x, mistLifetime.y),
            Mathf.Max(mistLifetime.x, mistLifetime.y));
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Min(mistStartSize.x, mistStartSize.y),
            Mathf.Max(mistStartSize.x, mistStartSize.y));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        Color a = colorA; a.a = mistAlpha;
        Color b = colorB; b.a = mistAlpha;
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.maxParticles = mistMaxParticles;

        var em = ps.emission;
        em.rateOverTime = mistEmissionRate;

        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = radius * mistRadiusMultiplier;
        sh.radiusThickness = 1f;                  // rellena todo el disco

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = mistRotationSpeed;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(FadeGradient());

        ps.Play();
    }

    // ---------- Utilidades ----------

    ParticleSystem MakeSystem(string objName, Material mat, out ParticleSystemRenderer r)
    {
        var go = new GameObject(objName);
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(root.transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = mat;
        return ps;
    }

    static Gradient FadeGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    Material GetAutoMaterial()
    {
        if (autoMaterial != null) return autoMaterial;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool urp = sh != null;
        if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        autoMaterial = new Material(sh) { hideFlags = HideFlags.DontSave };

        autoTexture = MakeSoftTexture(64);
        if (autoMaterial.HasProperty("_BaseMap")) autoMaterial.SetTexture("_BaseMap", autoTexture);
        if (autoMaterial.HasProperty("_MainTex")) autoMaterial.SetTexture("_MainTex", autoTexture);

        if (urp)
        {
            autoMaterial.SetFloat("_Surface", 1f);
            autoMaterial.SetFloat("_Blend", 2f);
            autoMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            autoMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            autoMaterial.SetFloat("_ZWrite", 0f);
            autoMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            autoMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return autoMaterial;
    }

    static Texture2D MakeSoftTexture(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                float a = d * d * (3f - 2f * d);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        return t;
    }
}