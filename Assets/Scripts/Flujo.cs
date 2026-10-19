using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
//  FLUJO: el "director" del juego.
//  Portada  ->  Niveles (selector)  ->  Mundo (explorar)  ->  cueva  ->  Nivel de plataformas
//  Se crea solo al darle Play. Maneja los cambios de pantalla con un fundido a negro.
// ============================================================================
public enum Pantalla { Portada, Niveles, Mundo, Plataforma }

public class Flujo : MonoBehaviour
{
    // ============ AJUSTES FACILES ============
    public const bool AUTO_INICIAR = true;
    public const Pantalla PANTALLA_INICIAL = Pantalla.Portada;
    public const float VELOCIDAD_FUNDIDO = 0.35f;   // segundos
    // =========================================

    public static Flujo I;
    public static bool Activo { get { return I != null; } }
    // true mientras hay fundido o pausa (Froxi no se mueve)
    public static bool Ocupado { get { return I != null && (I.cambiando || I.pausa); } }

    public Pantalla pantalla;
    bool cambiando, pausa;
    float fundido;                 // 0 = transparente, 1 = negro
    string textoCarga;
    GameObject actual;
    Camera cam;
    MundoEditable mundoEd;

    const float VH = 720f;
    float esc = 1f, vw = 1280f;
    readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    GUIStyle sTitulo, sTexto, sPeque, sCarga, sBotonPeque;
    float tPantalla;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Auto()
    {
        if (AUTO_INICIAR && I == null) new GameObject("Flujo").AddComponent<Flujo>();
    }

    void Awake()
    {
        I = this;
        cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        mundoEd = FindObjectOfType<MundoEditable>(true);
        // si la escena tiene un objeto "EscenaFroxi", abre esa pantalla o nivel; si no, la portada
#if UNITY_2023_1_OR_NEWER
        EscenaFroxi esc = FindFirstObjectByType<EscenaFroxi>();
#else
        EscenaFroxi esc = FindObjectOfType<EscenaFroxi>();
#endif
        if (esc != null) Montar(esc.pantalla, esc.nivel);
        else Montar(PANTALLA_INICIAL, 0);
    }

    void OnDestroy() { if (I == this) I = null; }

    // ------------------------------------------------------------ cambiar de pantalla
    public static void IrA(Pantalla p, int nivel = 0, string texto = null)
    {
        if (I == null || I.cambiando) return;
        I.StartCoroutine(I.Cambiar(p, nivel, texto));
    }

    // la llama la cueva
    public static void EntrarCueva()
    {
        int n = Progreso.SiguienteNivel();
        string nombre = Niveles.Todos[n].nombre;
        IrA(Pantalla.Plataforma, n, "Froxi entra a la cueva...\nNivel " + (n + 1) + ": " + nombre);
    }

    IEnumerator Cambiar(Pantalla p, int nivel, string texto)
    {
        cambiando = true;
        textoCarga = texto;
        yield return Fundir(1f);

        pausa = false;
        Time.timeScale = 1f;
        Limpiar();
        yield return null;                 // deja que Unity borre lo anterior
        Montar(p, nivel);

        if (texto != null)
        {
            float t = 0f;
            while (t < 1.2f) { t += Time.unscaledDeltaTime; yield return null; }
        }
        yield return Fundir(0f);
        textoCarga = null;
        cambiando = false;
    }

    IEnumerator Fundir(float destino)
    {
        while (!Mathf.Approximately(fundido, destino))
        {
            fundido = Mathf.MoveTowards(fundido, destino, Time.unscaledDeltaTime / VELOCIDAD_FUNDIDO);
            yield return null;
        }
    }

    void Limpiar()
    {
        if (actual != null) Destroy(actual);
        actual = null;
    }

    void Montar(Pantalla p, int nivel)
    {
        pantalla = p;
        tPantalla = Time.unscaledTime;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.18f, 0.13f);
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // si la escena tiene el mundo editable (objeto "Mundo" creado con Froxi > Mundo), se muestra solo en la pantalla Mundo
        if (mundoEd != null) mundoEd.gameObject.SetActive(p == Pantalla.Mundo);

        if (p == Pantalla.Mundo)
        {
            if (mundoEd == null)
            {
                actual = new GameObject("MundoJuego");     // sin mundo editable: se arma por codigo
                actual.AddComponent<MundoJuego>();
            }
        }
        else if (p == Pantalla.Plataforma)
        {
            actual = new GameObject("FroxiGame");
            FroxiGame fg = actual.AddComponent<FroxiGame>();
            fg.Empezar(nivel);
        }
    }

    void Update()
    {
        if (cambiando) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pantalla == Pantalla.Mundo) { pausa = !pausa; Time.timeScale = pausa ? 0f : 1f; }
            else if (pantalla == Pantalla.Niveles) IrA(Pantalla.Portada);
        }
        if (pantalla == Pantalla.Niveles && Input.GetKeyDown(KeyCode.U)) Progreso.DesbloquearTodo();
    }

    // ------------------------------------------------------------ dibujo
    Sprite S(string n)
    {
        Sprite s;
        if (!cache.TryGetValue(n, out s))
        {
            s = Resources.Load<Sprite>("UI/" + n);
            if (s == null) s = Resources.Load<Sprite>("Sprites/" + n);
            if (s == null) Debug.LogError("Falta la imagen UI: " + n + " (Assets/Resources/UI)");
            cache[n] = s;
        }
        return s;
    }

    void Dib(Rect r, string n)
    {
        Sprite s = S(n);
        if (s != null) GUI.DrawTexture(r, s.texture, ScaleMode.StretchToFill, true);
    }

    // dibuja un sprite de ancho w*k, centrado en cx
    Rect Centrado(string n, float cx, float y, float k)
    {
        Sprite s = S(n);
        float w = s != null ? s.rect.width * k : 0f, h = s != null ? s.rect.height * k : 0f;
        return new Rect(cx - w / 2f, y, w, h);
    }

    // marco que se estira sin deformar las esquinas (imagen de 48x48 con esquinas de 16)
    void Panel9(Rect r, string n, float borde)
    {
        Sprite s = S(n);
        if (s == null) return;
        Texture2D t = s.texture;
        float u = 16f / 48f;
        float[] xs = { r.x, r.x + borde, r.xMax - borde, r.xMax };
        float[] ys = { r.y, r.y + borde, r.yMax - borde, r.yMax };
        float[] us = { 0f, u, 1f - u, 1f };
        for (int iy = 0; iy < 3; iy++)
            for (int ix = 0; ix < 3; ix++)
            {
                Rect d = new Rect(xs[ix], ys[iy], xs[ix + 1] - xs[ix], ys[iy + 1] - ys[iy]);
                Rect uv = new Rect(us[ix], 1f - us[iy + 1], us[ix + 1] - us[ix], us[iy + 1] - us[iy]);
                GUI.DrawTextureWithTexCoords(d, t, uv);
            }
    }

    void Negro(Rect r, float a)
    {
        Color v = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, a);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = v;
    }

    // boton con imagen (nombre = "jugar", "niveles"...). Devuelve true si lo tocan.
    bool Boton(string nombre, float cx, float y, float k)
    {
        Sprite s = S("ui_boton_" + nombre + "_normal");
        if (s == null) return false;
        Rect r = new Rect(cx - s.rect.width * k / 2f, y, s.rect.width * k, s.rect.height * k);
        bool encima = !cambiando && r.Contains(Event.current.mousePosition);
        Rect d = r;
        if (encima) d.y -= 3f;
        Dib(d, "ui_boton_" + nombre + (encima ? "_hover" : "_normal"));
        return !cambiando && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void Estilos()
    {
        if (sTitulo != null) return;
        sTitulo = new GUIStyle(GUI.skin.label); sTitulo.fontStyle = FontStyle.Bold; sTitulo.alignment = TextAnchor.MiddleCenter; sTitulo.wordWrap = true;
        sTexto = new GUIStyle(sTitulo);
        sPeque = new GUIStyle(sTitulo);
        sCarga = new GUIStyle(sTitulo);
        sBotonPeque = new GUIStyle(GUI.skin.button); sBotonPeque.fontStyle = FontStyle.Bold;
    }

    void Texto(Rect r, string t, GUIStyle s, int tam, Color c)
    {
        s.fontSize = tam;
        s.normal.textColor = new Color(0f, 0f, 0f, 0.6f * c.a);
        Rect o = r; o.x += 2f; o.y += 2f;
        GUI.Label(o, t, s);
        s.normal.textColor = c;
        GUI.Label(r, t, s);
    }

    void OnGUI()
    {
        GUI.depth = -100;                        // siempre encima de todo
        Estilos();
        esc = Screen.height / VH;
        vw = Screen.width / esc;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1f));

        if (pantalla == Pantalla.Portada) DibujarPortada();
        else if (pantalla == Pantalla.Niveles) DibujarNiveles();
        else if (pantalla == Pantalla.Mundo && pausa) DibujarPausa();

        if (fundido > 0.001f)
        {
            Negro(new Rect(0, 0, vw, VH), fundido);
            if (textoCarga != null && fundido > 0.9f)
                Texto(new Rect(0, 250, vw, 200), textoCarga, sCarga, 44, Color.white);
        }
        GUI.matrix = Matrix4x4.identity;
    }

    // fondo: el mapa del mundo desplazandose despacio, con bordes oscuros
    void Fondo(float oscuro)
    {
        Sprite bg = S("ui_fondo_portada");
        if (bg != null)
        {
            const float K = 2f;                              // cada pixel del mapa = 2 unidades
            float anchoVis = vw / K, altoVis = VH / K;       // cuanto del mapa se ve
            float rangoX = Mathf.Max(0f, 1280f - anchoVis);
            float rangoY = Mathf.Max(0f, 1024f - altoVis);
            float t = (Time.unscaledTime - tPantalla) * 0.02f;
            float px = Mathf.PingPong(t * rangoX * 0.5f + rangoX * 0.35f, rangoX);
            float py = rangoY * 0.02f + Mathf.Sin(t * 3f) * 10f + 10f;
            Texture2D tex = bg.texture;
            Rect uv = new Rect(px / 1280f, 1f - (py + altoVis) / 1024f, anchoVis / 1280f, altoVis / 1024f);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, vw, VH), tex, uv);
        }
        Negro(new Rect(0, 0, vw, VH), oscuro);
        for (int i = 0; i < 8; i++)                          // vineta
        {
            float a = 0.35f * (1f - i / 8f);
            Negro(new Rect(0, i * 14f, vw, 14f), a);
            Negro(new Rect(0, VH - (i + 1) * 14f, vw, 14f), a);
        }
    }

    // ------------------------------------------------------------ PORTADA
    void DibujarPortada()
    {
        Fondo(0.12f);

        Sprite logo = S("ui_logo_froxi");
        float kl = logo != null ? Mathf.Min(4f, (vw - 60f) / logo.rect.width) : 4f;
        float rebote = Mathf.Sin(Time.unscaledTime * 2f) * 5f;
        Rect rl = Centrado("ui_logo_froxi", vw / 2f, 24f + rebote, kl);
        Dib(rl, "ui_logo_froxi");

        // Froxi saltando en su lugar + botones
        float ancho = 224f + 50f + 384f;
        float x0 = (vw - ancho) / 2f;
        int cuadro = (int)(Time.unscaledTime * 4f) % 4;
        Sprite fr = S("froxi_frente_" + cuadro);
        if (fr != null)
        {
            float salto = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * 18f;
            GUI.DrawTexture(new Rect(x0, 430f - salto, 224f, 224f), fr.texture, ScaleMode.StretchToFill, true);
        }
        float bx = x0 + 224f + 50f + 192f;
        if (Boton("jugar", bx, 345f, 3f)) IrA(Pantalla.Mundo);
        if (Boton("niveles", bx, 345f + 108f, 3f)) IrA(Pantalla.Niveles);
        if (Boton("salir", bx, 345f + 216f, 3f)) Salir();

        Texto(new Rect(0, VH - 44f, vw, 36f), "CBTis 75  •  Flechas o WASD: moverse  •  Esc: pausa", sPeque, 20, new Color(1f, 1f, 1f, 0.9f));
    }

    static void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------ SELECTOR DE NIVELES
    void DibujarNiveles()
    {
        Fondo(0.45f);
        Dib(Centrado("ui_cinta_elige_nivel", vw / 2f, 22f, 3f), "ui_cinta_elige_nivel");

        int n = Niveles.Todos.Length + 1;                    // +1 = el mundo
        float k = Mathf.Min(3f, (vw - 60f) / (n * 96f + (n - 1) * 16f), 440f / 128f);
        float cw = 96f * k, chh = 128f * k, gap = 16f * k;
        float total = n * cw + (n - 1) * gap;
        float x = (vw - total) / 2f, y = 150f;

        for (int i = -1; i < Niveles.Todos.Length; i++)
        {
            bool mundo = i < 0;
            bool abierto = mundo || Progreso.Abierto(i);
            Rect r = new Rect(x, y, cw, chh);
            bool encima = abierto && !cambiando && r.Contains(Event.current.mousePosition);
            Rect d = r; if (encima) d.y -= 6f;

            Dib(d, abierto ? (encima ? "ui_tarjeta_hover" : "ui_tarjeta_abierta") : "ui_tarjeta_bloqueada");

            Rect mini = new Rect(d.x + 8f * k, d.y + 8f * k, 80f * k, 48f * k);
            Color antes = GUI.color;
            if (!abierto) GUI.color = new Color(0.35f, 0.35f, 0.4f, 1f);
            Dib(mini, mundo ? "ui_mini_mundo" : (i < 3 ? "ui_mini_nivel" + (i + 1) : "ui_mini_nivel3"));
            GUI.color = antes;
            if (!abierto) Dib(new Rect(mini.center.x - 16f * k, mini.center.y - 16f * k, 32f * k, 32f * k), "ui_candado");

            Color tinta = new Color(0.17f, 0.12f, 0.28f);
            Texto(new Rect(d.x, d.y + 58f * k, d.width, 20f * k), mundo ? "MUNDO" : "NIVEL " + (i + 1), sTexto, Mathf.RoundToInt(9f * k), tinta);
            string sub = mundo ? "Explora y busca la cueva" : (abierto ? Niveles.Todos[i].nombre : "Bloqueado");
            Texto(new Rect(d.x + 4f * k, d.y + 78f * k, d.width - 8f * k, 20f * k), sub, sPeque, Mathf.RoundToInt(6.2f * k), new Color(0.3f, 0.25f, 0.4f));

            if (mundo)
                Dib(new Rect(d.x + 36f * k, d.y + 102f * k, 24f * k, 24f * k), "ui_icono_cueva");
            else
            {
                int est = Progreso.Estrellas(i);
                for (int e = 0; e < 3; e++)
                    Dib(new Rect(d.x + (22f + e * 18f) * k, d.y + 106f * k, 16f * k, 16f * k), e < est ? "ui_estrella_llena" : "ui_estrella_vacia");
            }

            if (abierto && !cambiando && GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (mundo) IrA(Pantalla.Mundo);
                else IrA(Pantalla.Plataforma, i, "Nivel " + (i + 1) + ": " + Niveles.Todos[i].nombre);
            }
            x += cw + gap;
        }

        if (Boton("volver", vw / 2f, VH - 100f, 2.5f)) IrA(Pantalla.Portada);

        Texto(new Rect(0, VH - 30f, vw, 26f), "Tecla U: abrir todos los niveles (para pruebas)", sPeque, 16, new Color(1f, 1f, 1f, 0.7f));
        if (GUI.Button(new Rect(vw - 190f, VH - 52f, 170f, 36f), "Borrar progreso", sBotonPeque)) Progreso.Borrar();
    }

    // ------------------------------------------------------------ PAUSA (en el mundo)
    void DibujarPausa()
    {
        Negro(new Rect(0, 0, vw, VH), 0.55f);
        Rect p = new Rect(vw / 2f - 260f, 150f, 520f, 420f);
        Panel9(p, "ui_panel_9", 48f);
        Dib(Centrado("ui_cinta_pausa", vw / 2f, 110f, 3f), "ui_cinta_pausa");
        if (Boton("continuar", vw / 2f, 250f, 3f)) { pausa = false; Time.timeScale = 1f; }
        if (Boton("menu", vw / 2f, 370f, 3f)) IrA(Pantalla.Portada);
    }
}
