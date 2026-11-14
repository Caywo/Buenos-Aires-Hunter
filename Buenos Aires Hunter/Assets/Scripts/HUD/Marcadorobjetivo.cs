using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class MarcadorObjetivo : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Se puede dejar vacío: si el objetivo tiene este Tag, el marcador lo busca solo.")]
    [SerializeField] Transform objetivo;
    [SerializeField] string tagObjetivo = "Extraccion";
    [Tooltip("Punto sobre el objetivo donde flota el marcador (metros).")]
    [SerializeField] Vector3 offsetMundo = new Vector3(0f, 3f, 0f);

    [Header("Elementos")]
    [Tooltip("Flecha que rota. Por defecto el sprite tiene que apuntar HACIA ARRIBA.")]
    [SerializeField] RectTransform flecha;
    [Tooltip("Tildalo si el sprite de la flecha ya está dibujado apuntando HACIA ABAJO.")]
    [SerializeField] bool spriteApuntaAbajo = false;
    [SerializeField] TMP_Text txtDistancia;

    [Header("Pantalla")]
    [Tooltip("Distancia mínima al borde de la pantalla, en píxeles. Subila si el texto se corta.")]
    [SerializeField] float margen = 110f;

    [Header("Tamaño")]
    [Tooltip("Tamaño cuando el objetivo está fuera de pantalla (pegado al borde).")]
    [SerializeField] float escalaFuera = 0.5f;
    [Tooltip("Tamaño cuando el objetivo está a la vista, cerca de los bordes.")]
    [SerializeField] float escalaEnPantalla = 0.9f;
    [Tooltip("Tamaño cuando el jugador mira directamente al objetivo (centro de la pantalla).")]
    [SerializeField] float escalaMirando = 1.25f;
    [Tooltip("Qué tan cerca del centro cuenta como 'mirándolo' (0.25 = un cuarto de la altura de la pantalla).")]
    [SerializeField, Range(0.05f, 1f)] float radioMirando = 0.25f;
    [SerializeField] float velocidadEscala = 10f;

    [Header("Transparencia por distancia")]
    [SerializeField, Range(0f, 1f)] float alphaCerca = 1f;
    [SerializeField, Range(0f, 1f)] float alphaLejos = 0.35f;
    [SerializeField] float distanciaCerca = 40f;
    [SerializeField] float distanciaLejos = 150f;

    [Header("Cuando está detrás de una pared")]
    [SerializeField, Range(0f, 1f)] float multiplicadorOculto = 0.4f;
    [Tooltip("Capas que cuentan como pared. Destildá Player y Enemy.")]
    [SerializeField] LayerMask capasOclusion = ~0;

    [Header("Al llegar")]
    [Tooltip("Por debajo de esta distancia se desvanece. 0 = nunca se oculta.")]
    [SerializeField] float distanciaOcultar = 8f;

    [Header("Animación")]
    [SerializeField] float velocidadFade = 8f;

    CanvasGroup grupo;
    RectTransform rect;
    RectTransform rectPadre;

    float proximaBusqueda;
    float proximaOclusion;
    bool ocluido;
    bool avisoTag;
    int ultimaDistancia = -1;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        rectPadre = transform.parent as RectTransform;

        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;   // el marcador nunca tapa clics
    }

    // Opcional: para asignar el objetivo por código en vez de usar el Tag
    public void SetObjetivo(Transform nuevo) => objetivo = nuevo;

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        if (objetivo == null)
        {
            BuscarObjetivo();
            Desvanecer(0f, dt);
            return;
        }

        Camera cam = Camera.main;
        if (cam == null || rectPadre == null)
        {
            Desvanecer(0f, dt);
            return;
        }

        // ── Posición en pantalla ──
        float ancho = cam.pixelWidth;
        float alto = cam.pixelHeight;
        Vector2 centro = new Vector2(ancho, alto) * 0.5f;

        Vector3 sp = cam.WorldToScreenPoint(objetivo.position + offsetMundo);
        bool detras = sp.z < 0f;

        Vector2 pos = sp;
        if (detras) pos = new Vector2(ancho - pos.x, alto - pos.y);   // detrás de la cámara, Unity devuelve la posición invertida

        bool dentro = !detras
                      && pos.x > margen && pos.x < ancho - margen
                      && pos.y > margen && pos.y < alto - margen;

        Vector2 dir = pos - centro;

        if (!dentro)
        {
            // Lo pega al borde de la pantalla, en la dirección del objetivo
            if (dir.sqrMagnitude < 1f) dir = Vector2.down;

            float escalaX = (ancho * 0.5f - margen) / Mathf.Max(0.01f, Mathf.Abs(dir.x));
            float escalaY = (alto * 0.5f - margen) / Mathf.Max(0.01f, Mathf.Abs(dir.y));
            pos = centro + dir * Mathf.Min(escalaX, escalaY);
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectPadre, pos, null, out Vector2 local);
        rect.localPosition = new Vector3(local.x, local.y, 0f);

        // ── Tamaño ──
        float escalaObjetivo;
        if (!dentro)
        {
            escalaObjetivo = escalaFuera;
        }
        else
        {
            // 0 = centro de la pantalla, 1 = borde de arriba o de abajo
            float distCentro = Vector2.Distance(pos, centro) / (alto * 0.5f);
            float cercaDelCentro = 1f - Mathf.InverseLerp(radioMirando, 1f, distCentro);
            escalaObjetivo = Mathf.Lerp(escalaEnPantalla, escalaMirando, cercaDelCentro);
        }

        float escalaActual = Mathf.Lerp(rect.localScale.x, escalaObjetivo, 1f - Mathf.Exp(-velocidadEscala * dt));
        rect.localScale = new Vector3(escalaActual, escalaActual, 1f);

        // ── Flecha ──
        if (flecha != null)
        {
            float angulo;
            if (dentro)
            {
                angulo = spriteApuntaAbajo ? 0f : 180f;
            }
            else
            {
                angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                if (spriteApuntaAbajo) angulo += 180f;
            }

            flecha.localRotation = Quaternion.Euler(0f, 0f, angulo);
        }

        // ── Distancia ──
        float distancia = Vector3.Distance(cam.transform.position, objetivo.position);
        ActualizarTextoDistancia(distancia);

        // ── Transparencia ──
        float t = Mathf.InverseLerp(distanciaCerca, distanciaLejos, distancia);
        float alpha = Mathf.Lerp(alphaCerca, alphaLejos, t);

        if (distanciaOcultar > 0f)
            alpha *= Mathf.InverseLerp(distanciaOcultar, distanciaOcultar * 1.5f, distancia);

        if (dentro && HayParedEnElMedio(cam))
            alpha *= multiplicadorOculto;

        Desvanecer(alpha, dt);
    }

    void Desvanecer(float objetivoAlpha, float dt)
    {
        grupo.alpha = Mathf.Lerp(grupo.alpha, objetivoAlpha, 1f - Mathf.Exp(-velocidadFade * dt));
    }

    void ActualizarTextoDistancia(float distancia)
    {
        if (txtDistancia == null) return;

        int metros = Mathf.RoundToInt(distancia);
        if (metros == ultimaDistancia) return;   // no reescribe el texto si no cambió

        ultimaDistancia = metros;
        txtDistancia.text = metros + " m";
    }

    // Solo se calcula 10 veces por segundo, no en cada frame
    bool HayParedEnElMedio(Camera cam)
    {
        if (Time.unscaledTime < proximaOclusion) return ocluido;
        proximaOclusion = Time.unscaledTime + 0.1f;

        Vector3 desde = cam.transform.position;
        Vector3 hasta = objetivo.position + Vector3.up * 1.5f;

        if (Physics.Linecast(desde, hasta, out RaycastHit golpe, capasOclusion, QueryTriggerInteraction.Ignore))
            ocluido = !golpe.transform.IsChildOf(objetivo);   // chocar con el propio colectivo no cuenta
        else
            ocluido = false;

        return ocluido;
    }

    void BuscarObjetivo()
    {
        if (string.IsNullOrEmpty(tagObjetivo) || Time.unscaledTime < proximaBusqueda) return;
        proximaBusqueda = Time.unscaledTime + 1f;

        try
        {
            GameObject go = GameObject.FindGameObjectWithTag(tagObjetivo);
            if (go != null) objetivo = go.transform;
        }
        catch (UnityException)
        {
            if (!avisoTag)
            {
                avisoTag = true;
                Debug.LogWarning("[MarcadorObjetivo] El Tag '" + tagObjetivo + "' no existe. Crealo en Tags & Layers.");
            }
        }
    }
}