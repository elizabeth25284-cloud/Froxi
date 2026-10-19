using System.Collections.Generic;
using UnityEngine;

public enum Estado { Menu, Jugando, Pregunta, Retroalimentacion, Pausa, FinNivel, GameOver, Victoria }

// Este script se crea SOLO al darle Play (no tienes que ponerlo en ningun objeto).
// Construye el nivel, la camara, el menu, los puntos, las vidas y las preguntas.
public class FroxiGame : MonoBehaviour
{
    public static FroxiGame I;

    // ============ AJUSTES FACILES (cambialos a tu gusto) ============
    public const int VIDAS_INICIALES = 3;
    public const int PUNTOS_SALUDABLE = 10;          // por cada alimento saludable
    public const int PUNTOS_RESPUESTA_CORRECTA = 20; // por contestar bien
    public const int PUNTOS_PISAR_CHATARRA = 5;      // por saltarle encima a la chatarra
    public const int BONUS_POR_VIDA = 10;            // al terminar el nivel, por cada vida
    public const bool PISAR_VENCE_CHATARRA = true;   // false = siempre sale la pregunta
    public const float SEGUNDOS_INVULNERABLE = 2f;
    // ================================================================

    public Estado estado = Estado.Menu;
    public int puntos, vidas, nivelActual, saludables, totalSaludables;
    public JugadorFroxi jugador;

    Camera cam;
    GameObject raiz;
    Transform fondoT;
    int W, H;
    float cx0, introHasta;
    int puntosInicioNivel;
    NivelDatos nivel;
    readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // pregunta
    Pregunta preguntaActual;
    int[] orden = new int[] { 0, 1, 2 };
    bool ultimaCorrecta;
    ComidaChatarra chatarraPendiente;
    readonly List<int> pool = new List<int>();

    // textos flotantes (+10)
    class Flotante { public Vector3 pos; public string txt; public float t; public Color color; }
    readonly List<Flotante> flot = new List<Flotante>();

    // En el kit completo el que arranca todo es "Flujo" (portada, niveles, mundo).
    // Si quieres probar SOLO el juego de plataformas, pon true.
    public const bool AUTO_INICIAR = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (AUTO_INICIAR && I == null) new GameObject("FroxiGame").AddComponent<FroxiGame>();
    }

    // La llama Flujo para empezar directamente en un nivel
    public void Empezar(int nivelInicial) { NuevaPartida(nivelInicial); }

    void OnDestroy()
    {
        LimpiarNivel();
        Time.timeScale = 1f;
        if (I == this) I = null;
    }

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        Physics2D.queriesStartInColliders = false;
        Physics2D.queriesHitTriggers = false;

        cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.72f, 0.92f);
        cam.transform.position = new Vector3(0f, 4.5f, -10f);
        estado = Estado.Menu;
    }

    // ---------------------------------------------------------------- sprites
    public Sprite S(string n)
    {
        Sprite s;
        if (!cache.TryGetValue(n, out s))
        {
            s = Resources.Load<Sprite>("Sprites/" + n);
            if (s == null) Debug.LogError("Falta el sprite: Assets/Resources/Sprites/" + n + ".png");
            cache[n] = s;
        }
        return s;
    }

    public Sprite[] Cuadros(string prefijo, int n)
    {
        Sprite[] a = new Sprite[n];
        for (int i = 0; i < n; i++) a[i] = S(prefijo + "_" + i);
        return a;
    }

    // ---------------------------------------------------------------- flujo del juego
    void NuevaPartida(int nivelInicial)
    {
        puntos = 0;
        nivelActual = nivelInicial;
        CargarNivel(nivelActual);
    }

    void LimpiarNivel()
    {
        if (raiz != null) Destroy(raiz);
        raiz = null; jugador = null; fondoT = null;
        flot.Clear();
    }

    void IrAlMenu()
    {
        if (Flujo.Activo) { Flujo.IrA(Pantalla.Portada); return; }
        LimpiarNivel();
        estado = Estado.Menu;
        Time.timeScale = 1f;
        cam.transform.position = new Vector3(0f, 4.5f, -10f);
    }

    GameObject Nuevo(string nombre, Vector3 pos)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(raiz.transform);
        g.transform.position = pos;
        return g;
    }

    SpriteRenderer ConSprite(GameObject g, Sprite s, int capa)
    {
        SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = capa;
        return sr;
    }

    char Tile(int c, int r)
    {
        if (r < 0 || r >= H || c < 0 || c >= W) return '.';
        return nivel.mapa[r][c];
    }

    Vector3 Pos(int c, int r) { return new Vector3(c, H - 1 - r, 0f); }

    void CargarNivel(int idx)
    {
        LimpiarNivel();
        nivel = Niveles.Todos[idx];
        H = nivel.mapa.Length;
        W = nivel.mapa[0].Length;
        raiz = new GameObject("Nivel");
        saludables = 0; totalSaludables = 0;
        vidas = VIDAS_INICIALES;
        puntosInicioNivel = puntos;
        chatarraPendiente = null;

        // ---- fondo con parallax
        Sprite fs = S(nivel.fondo);
        GameObject fondo = Nuevo("Fondo", new Vector3(W / 2f, (H - 1) / 2f, 0f));
        SpriteRenderer fsr = ConSprite(fondo, fs, -10);
        fsr.drawMode = SpriteDrawMode.Tiled;
        fsr.size = new Vector2(W * 2f + 60f, fs.bounds.size.y);
        fondoT = fondo.transform;
        cx0 = W / 2f;

        // ---- suelo y plataformas (se juntan en tiras para que sea ligero)
        for (int r = 0; r < H; r++)
        {
            int c = 0;
            while (c < W)
            {
                char ch = Tile(c, r);
                if (ch == '#' || ch == '=')
                {
                    int c0 = c;
                    while (c < W && Tile(c, r) == ch) c++;
                    if (ch == '#') CrearSuelo(c0, c - c0, r); else CrearPlataforma(c0, c - c0, r);
                }
                else c++;
            }
        }

        // ---- bloques solidos
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (Tile(c, r) == 'B')
                {
                    GameObject b = Nuevo("Bloque", Pos(c, r));
                    ConSprite(b, S(nivel.bloque), 1);
                    b.AddComponent<BoxCollider2D>().size = Vector2.one;
                }

        // ---- paredes invisibles a los lados
        CrearPared(-1.0f); CrearPared(W);

        // ---- Froxi primero (los enemigos lo necesitan)
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (Tile(c, r) == 'P') CrearJugador(c, r);
        if (jugador == null) CrearJugador(2, H - 3);

        // ---- comida, enemigos y meta
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char ch = Tile(c, r);
                if (ch == 'f') CrearSaludable(c, r);
                else if (ch == 'j' || ch == 'x') CrearChatarra(c, r, ch == 'j');
                else if (ch == 'G') CrearMeta(c, r);
            }

        estado = Estado.Jugando;
        Time.timeScale = 1f;
        introHasta = Time.unscaledTime + 3.5f;
        PosicionarCamara(true);
    }

    void CrearSuelo(int c0, int largo, int r)
    {
        GameObject g = Nuevo("Suelo", new Vector3(c0 + (largo - 1) / 2f, H - 1 - r, 0f));
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(largo, 1f);
        for (int k = 0; k < largo; k++)
        {
            GameObject t = new GameObject("t");
            t.transform.SetParent(g.transform);
            t.transform.position = Pos(c0 + k, r);
            bool arriba = Tile(c0 + k, r - 1) == '#';
            ConSprite(t, S(arriba ? nivel.cuerpo : nivel.top), 0);
        }
    }

    void CrearPlataforma(int c0, int largo, int r)
    {
        GameObject g = Nuevo("Plataforma", new Vector3(c0 + (largo - 1) / 2f, H - 1 - r, 0f));
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(largo, 0.3f);
        bc.offset = new Vector2(0f, 0.35f);
        PlatformEffector2D ef = g.AddComponent<PlatformEffector2D>();
        ef.useOneWay = true;
        ef.surfaceArc = 160f;
        ef.useSideFriction = false;
        bc.usedByEffector = true;
        for (int k = 0; k < largo; k++)
        {
            GameObject t = new GameObject("t");
            t.transform.SetParent(g.transform);
            t.transform.position = Pos(c0 + k, r);
            string n = nivel.plataformaM;
            if (largo > 1 && k == 0) n = nivel.plataformaL;
            else if (largo > 1 && k == largo - 1) n = nivel.plataformaR;
            ConSprite(t, S(n), 1);
        }
    }

    void CrearPared(float x)
    {
        GameObject g = Nuevo("Pared", new Vector3(x, (H - 1) / 2f, 0f));
        g.AddComponent<BoxCollider2D>().size = new Vector2(1f, 60f);
    }

    void CrearJugador(int c, int r)
    {
        GameObject g = Nuevo("Froxi", Pos(c, r));
        SpriteRenderer sr = ConSprite(g, S("froxi_der_1"), 5);
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.6f, 0.75f);
        bc.offset = new Vector2(0f, -0.095f);
        PhysicsMaterial2D mat = new PhysicsMaterial2D("sinFriccion");
        mat.friction = 0f; mat.bounciness = 0f;
        bc.sharedMaterial = mat;
        jugador = g.AddComponent<JugadorFroxi>();
        jugador.caminar = Cuadros("froxi_der", 4);
        jugador.ultimaSegura = g.transform.position;
    }

    void CrearSaludable(int c, int r)
    {
        string[] n = { "saludable_manzana", "saludable_brocoli", "saludable_zanahoria", "saludable_fresa" };
        GameObject g = Nuevo("Saludable", Pos(c, r));
        g.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        ConSprite(g, S(n[(c + r) % 4] + "_0"), 3);
        AnimacionSprite an = g.AddComponent<AnimacionSprite>();
        an.cuadros = Cuadros(n[(c + r) % 4], 4);
        an.fps = 5f;
        CircleCollider2D cc = g.AddComponent<CircleCollider2D>();
        cc.radius = 0.45f; cc.isTrigger = true;
        g.AddComponent<ComidaSaludable>();
        totalSaludables++;
    }

    void CrearChatarra(int c, int r, bool camina)
    {
        string[] n = { "chatarra_hamburguesa", "chatarra_papas", "chatarra_dona", "chatarra_refresco" };
        string nom = n[(c * 3 + r) % 4];
        GameObject g = Nuevo("Chatarra", Pos(c, r));
        ConSprite(g, S(nom + "_0"), 4);
        AnimacionSprite an = g.AddComponent<AnimacionSprite>();
        an.cuadros = Cuadros(nom, 4);
        an.fps = camina ? 6f : 3f;
        Rigidbody2D rb = g.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.7f, 0.8f);
        bc.offset = new Vector2(0f, -0.05f);
        PhysicsMaterial2D mat = new PhysicsMaterial2D("sinFriccion");
        mat.friction = 0f; mat.bounciness = 0f;
        bc.sharedMaterial = mat;
        ComidaChatarra cc = g.AddComponent<ComidaChatarra>();
        cc.patrulla = camina;
        if (jugador != null) Physics2D.IgnoreCollision(jugador.col, bc);
    }

    void CrearMeta(int c, int r)
    {
        Sprite sp = S(nivel.meta);
        int rr = r;
        while (rr < H - 1 && Tile(c, rr) != '#') rr++;
        float superficie = (H - 1 - rr) + 0.5f;
        float ancho = sp.bounds.size.x, alto = sp.bounds.size.y;
        GameObject g = Nuevo("Meta", new Vector3(c + (ancho - 1f) / 2f, superficie + alto / 2f, 0f));
        ConSprite(g, sp, 2);
        BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(Mathf.Min(1.4f, ancho), alto);
        g.AddComponent<MetaNivel>();
    }

    // ---------------------------------------------------------------- eventos del juego
    public void RecogerSaludable(ComidaSaludable f)
    {
        if (estado != Estado.Jugando) return;
        puntos += PUNTOS_SALUDABLE;
        saludables++;
        Flota(f.transform.position, "+" + PUNTOS_SALUDABLE, new Color(1f, 0.92f, 0.3f));
        Destroy(f.gameObject);
    }

    public void ContactoChatarra(ComidaChatarra c, bool pisada)
    {
        if (estado != Estado.Jugando || jugador == null || jugador.invulnerable) return;

        if (pisada && PISAR_VENCE_CHATARRA)
        {
            puntos += PUNTOS_PISAR_CHATARRA;
            Flota(c.transform.position, "+" + PUNTOS_PISAR_CHATARRA, Color.white);
            jugador.Rebotar();
            Destroy(c.gameObject);
            return;
        }

        chatarraPendiente = c;
        c.Congelar();
        preguntaActual = SiguientePregunta();
        orden = new int[] { 0, 1, 2 };
        for (int i = 2; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = orden[i]; orden[i] = orden[j]; orden[j] = t;
        }
        estado = Estado.Pregunta;
        Time.timeScale = 0f;
    }

    Pregunta SiguientePregunta()
    {
        if (pool.Count == 0)
        {
            for (int i = 0; i < BancoPreguntas.Todas.Length; i++) pool.Add(i);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int t = pool[i]; pool[i] = pool[j]; pool[j] = t;
            }
        }
        int k = pool[pool.Count - 1];
        pool.RemoveAt(pool.Count - 1);
        return BancoPreguntas.Todas[k];
    }

    string Opcion(int i)
    {
        int o = orden[i];
        return o == 0 ? preguntaActual.correcta : (o == 1 ? preguntaActual.mala1 : preguntaActual.mala2);
    }

    void Responder(int i)
    {
        if (estado != Estado.Pregunta) return;
        ultimaCorrecta = orden[i] == 0;
        if (ultimaCorrecta) puntos += PUNTOS_RESPUESTA_CORRECTA;
        else vidas--;
        estado = Estado.Retroalimentacion;
    }

    void ContinuarTrasPregunta()
    {
        if (chatarraPendiente != null) Destroy(chatarraPendiente.gameObject);
        chatarraPendiente = null;
        if (vidas <= 0)
        {
            estado = Estado.GameOver;
            Time.timeScale = 0f;
            return;
        }
        estado = Estado.Jugando;
        Time.timeScale = 1f;
        if (jugador != null) jugador.Invulnerabilidad(SEGUNDOS_INVULNERABLE);
    }

    public void TerminarNivel()
    {
        if (estado != Estado.Jugando) return;
        puntos += vidas * BONUS_POR_VIDA;
        Progreso.Completar(nivelActual, vidas, puntos);
        estado = Estado.FinNivel;
        Time.timeScale = 0f;
    }

    void SiguienteNivel()
    {
        nivelActual++;
        if (nivelActual >= Niveles.Todos.Length)
        {
            estado = Estado.Victoria;
            Time.timeScale = 0f;
            return;
        }
        CargarNivel(nivelActual);
    }

    void ReintentarNivel()
    {
        puntos = puntosInicioNivel;
        CargarNivel(nivelActual);
    }

    void Flota(Vector3 p, string t, Color c)
    {
        Flotante f = new Flotante();
        f.pos = p; f.txt = t; f.color = c;
        flot.Add(f);
    }

    // ---------------------------------------------------------------- Update / camara
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (estado == Estado.Jugando) { estado = Estado.Pausa; Time.timeScale = 0f; }
            else if (estado == Estado.Pausa) { estado = Estado.Jugando; Time.timeScale = 1f; }
        }
        if (estado == Estado.Pregunta)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) Responder(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Responder(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Responder(2);
        }
        if (estado == Estado.Retroalimentacion && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z)))
            ContinuarTrasPregunta();

        for (int i = flot.Count - 1; i >= 0; i--)
        {
            flot[i].t += Time.deltaTime;
            flot[i].pos += Vector3.up * 1.2f * Time.deltaTime;
            if (flot[i].t > 1f) flot.RemoveAt(i);
        }
    }

    void LateUpdate()
    {
        if (raiz != null && jugador != null) PosicionarCamara(false);
    }

    void PosicionarCamara(bool brusco)
    {
        if (jugador == null) return;
        float mitad = cam.orthographicSize * cam.aspect;
        float tx = Mathf.Clamp(jugador.transform.position.x, mitad - 0.5f, W - 0.5f - mitad);
        if (W < mitad * 2f) tx = (W - 1) / 2f;
        float x = cam.transform.position.x;
        x = brusco ? tx : Mathf.Lerp(x, tx, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        cam.transform.position = new Vector3(x, (H - 1) / 2f, -10f);
        if (fondoT != null)
            fondoT.position = new Vector3(cx0 + (x - cx0) * 0.6f, (H - 1) / 2f, 0f);
    }

    // ================================================================ INTERFAZ (OnGUI)
    GUIStyle sTitulo, sEtiqueta, sParrafo, sBoton, sOpcion, sCentro;
    float esc, vw;
    const float VH = 720f;

    void Estilos()
    {
        if (sEtiqueta != null) return;
        sEtiqueta = new GUIStyle(GUI.skin.label); sEtiqueta.fontSize = 28; sEtiqueta.fontStyle = FontStyle.Bold; sEtiqueta.alignment = TextAnchor.MiddleLeft;
        sCentro = new GUIStyle(GUI.skin.label); sCentro.fontSize = 30; sCentro.fontStyle = FontStyle.Bold; sCentro.alignment = TextAnchor.MiddleCenter; sCentro.wordWrap = true;
        sTitulo = new GUIStyle(GUI.skin.label); sTitulo.fontSize = 130; sTitulo.fontStyle = FontStyle.Bold; sTitulo.alignment = TextAnchor.MiddleCenter;
        sParrafo = new GUIStyle(GUI.skin.label); sParrafo.fontSize = 30; sParrafo.fontStyle = FontStyle.Bold; sParrafo.alignment = TextAnchor.MiddleCenter; sParrafo.wordWrap = true;
        sBoton = new GUIStyle(GUI.skin.button); sBoton.fontSize = 30; sBoton.fontStyle = FontStyle.Bold;
        sOpcion = new GUIStyle(GUI.skin.button); sOpcion.fontSize = 26; sOpcion.fontStyle = FontStyle.Bold; sOpcion.wordWrap = true; sOpcion.alignment = TextAnchor.MiddleLeft; sOpcion.padding = new RectOffset(24, 12, 6, 6);
    }

    void Texto(Rect r, string t, GUIStyle s, Color c)
    {
        Color viejo = s.normal.textColor;
        s.normal.textColor = new Color(0f, 0f, 0f, 0.75f * c.a);
        Rect o = r; o.x += 2f; o.y += 2f;
        GUI.Label(o, t, s);
        s.normal.textColor = c;
        GUI.Label(r, t, s);
        s.normal.textColor = viejo;
    }

    void TextoContorno(Rect r, string t, GUIStyle s, Color c, Color borde)
    {
        Color viejo = s.normal.textColor;
        s.normal.textColor = borde;
        for (int dx = -4; dx <= 4; dx += 4)
            for (int dy = -4; dy <= 4; dy += 4)
                if (dx != 0 || dy != 0) { Rect o = r; o.x += dx; o.y += dy; GUI.Label(o, t, s); }
        s.normal.textColor = c;
        GUI.Label(r, t, s);
        s.normal.textColor = viejo;
    }

    void Panel(Rect r, Color c)
    {
        Color v = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = v;
    }

    void DibujarSprite(Rect r, Sprite s)
    {
        if (s == null) return;
        Texture2D t = s.texture; Rect sr = s.rect;
        GUI.DrawTextureWithTexCoords(r, t, new Rect(sr.x / t.width, sr.y / t.height, sr.width / t.width, sr.height / t.height));
    }

    bool Boton(Rect r, string t, Color tinte)
    {
        Color v = GUI.backgroundColor; GUI.backgroundColor = tinte;
        bool b = GUI.Button(r, t, sBoton);
        GUI.backgroundColor = v;
        return b;
    }

    static readonly Color Verde = new Color(0.45f, 0.85f, 0.4f);
    static readonly Color Morado = new Color(0.7f, 0.5f, 0.95f);
    static readonly Color Rosa = new Color(1f, 0.55f, 0.7f);

    void OnGUI()
    {
        Estilos();
        esc = Screen.height / VH;
        vw = Screen.width / esc;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1f));

        switch (estado)
        {
            case Estado.Menu: UIMenu(); break;
            case Estado.Jugando: HUD(); UIIntro(); UIFlotantes(); break;
            case Estado.Pregunta: HUD(); UIPregunta(); break;
            case Estado.Retroalimentacion: HUD(); UIRetro(); break;
            case Estado.Pausa: HUD(); UIPausa(); break;
            case Estado.FinNivel: HUD(); UIFinNivel(); break;
            case Estado.GameOver: HUD(); UIGameOver(); break;
            case Estado.Victoria: UIVictoria(); break;
        }
        GUI.matrix = Matrix4x4.identity;
    }

    void UIMenu()
    {
        Sprite bg = S("fondo_calle");
        if (bg != null)
        {
            float alto = bg.texture.height, ancho = bg.texture.width;
            float uvw = Mathf.Min(1f, (vw / (VH / alto)) / ancho);
            GUI.DrawTextureWithTexCoords(new Rect(0, 0, vw, VH), bg.texture, new Rect(0.05f, 0f, uvw, 1f));
        }
        Panel(new Rect(0, 0, vw, VH), new Color(1f, 1f, 1f, 0.12f));
        TextoContorno(new Rect(0, 20, vw, 150), "FROXI", sTitulo, new Color(0.5f, 0.95f, 0.35f), new Color(0.1f, 0.3f, 0.15f));
        Texto(new Rect(0, 160, vw, 40), "Aventura de un pequeño héroe", sCentro, Color.white);

        DibujarSprite(new Rect(vw * 0.5f - 420f, 270f, 288f, 288f), S("froxi_frente_0"));

        float bx = vw * 0.5f - 60f;
        if (Boton(new Rect(bx, 250, 380, 68), "¡Jugar!", Verde)) NuevaPartida(0);
        for (int i = 0; i < Niveles.Todos.Length; i++)
            if (Boton(new Rect(bx, 334 + i * 66, 380, 56), "Nivel " + (i + 1) + ": " + Niveles.Todos[i].nombre, Morado)) NuevaPartida(i);
        if (Boton(new Rect(bx, 334 + Niveles.Todos.Length * 66 + 6, 380, 56), "Salir", Rosa)) Application.Quit();

        Panel(new Rect(0, VH - 70, vw, 70), new Color(0f, 0f, 0f, 0.45f));
        Texto(new Rect(0, VH - 70, vw, 70), "Flechas o A/D: mover   •   X, W o Espacio: saltar   •   C: pausa   •   Toca la comida chatarra y contesta bien para no perder vida", sParrafo, Color.white);
        sParrafo.fontSize = 30;
    }

    void HUD()
    {
        for (int i = 0; i < VIDAS_INICIALES; i++)
            DibujarSprite(new Rect(18 + i * 52, 14, 48, 48), S(i < vidas ? "ui_corazon" : "ui_corazon_vacio"));
        DibujarSprite(new Rect(vw - 280, 14, 48, 48), S("ui_estrella"));
        Texto(new Rect(vw - 226, 14, 220, 48), puntos + " pts", sEtiqueta, Color.white);
        Texto(new Rect(18, 66, 400, 40), "Saludables: " + saludables + "/" + totalSaludables, sEtiqueta, new Color(0.7f, 1f, 0.6f));
        Texto(new Rect(0, 14, vw, 48), "Nivel " + (nivelActual + 1) + ": " + (nivel != null ? nivel.nombre : ""), sCentro, Color.white);
    }

    void UIIntro()
    {
        float falta = introHasta - Time.unscaledTime;
        if (falta <= 0f || nivel == null) return;
        float a = Mathf.Clamp01(falta);
        Panel(new Rect(vw * 0.5f - 460, 260, 920, 170), new Color(0f, 0f, 0f, 0.55f * a));
        Texto(new Rect(vw * 0.5f - 440, 270, 880, 60), "Nivel " + (nivelActual + 1) + ": " + nivel.nombre, sCentro, new Color(1f, 1f, 1f, a));
        Texto(new Rect(vw * 0.5f - 440, 336, 880, 90), nivel.descripcion, sParrafo, new Color(1f, 1f, 0.7f, a));
    }

    void UIFlotantes()
    {
        for (int i = 0; i < flot.Count; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(flot[i].pos);
            Color c = flot[i].color; c.a = 1f - flot[i].t;
            Texto(new Rect(sp.x / esc - 70f, (Screen.height - sp.y) / esc - 25f, 140f, 50f), flot[i].txt, sCentro, c);
        }
    }

    void UIPregunta()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.6f));
        Rect p = new Rect((vw - 880f) / 2f, 100f, 880f, 540f);
        Panel(p, new Color(0.12f, 0.18f, 0.28f, 0.97f));
        Texto(new Rect(p.x, p.y + 12, p.width, 50), "¡Comida chatarra! Contesta bien para no perder una vida", sCentro, new Color(1f, 0.7f, 0.3f));
        Texto(new Rect(p.x + 30, p.y + 75, p.width - 60, 130), preguntaActual.texto, sParrafo, Color.white);
        for (int i = 0; i < 3; i++)
            if (GUI.Button(new Rect(p.x + 40, p.y + 225 + i * 90, p.width - 80, 76), (char)('A' + i) + ")  " + Opcion(i), sOpcion))
                Responder(i);
        Texto(new Rect(p.x, p.y + 495, p.width, 36), "Tip: también puedes pulsar 1, 2 o 3", sCentro, new Color(0.8f, 0.85f, 1f));
    }

    void UIRetro()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.6f));
        Rect p = new Rect((vw - 880f) / 2f, 130f, 880f, 460f);
        Panel(p, new Color(0.12f, 0.18f, 0.28f, 0.97f));
        if (ultimaCorrecta)
        {
            Texto(new Rect(p.x, p.y + 20, p.width, 60), "¡Correcto!  +" + PUNTOS_RESPUESTA_CORRECTA + " puntos", sCentro, new Color(0.5f, 1f, 0.5f));
            Texto(new Rect(p.x + 30, p.y + 90, p.width - 60, 50), "Conservas tu vida.", sParrafo, Color.white);
        }
        else
        {
            Texto(new Rect(p.x, p.y + 20, p.width, 60), "Incorrecto... pierdes una vida", sCentro, new Color(1f, 0.5f, 0.5f));
            Texto(new Rect(p.x + 30, p.y + 90, p.width - 60, 60), "Respuesta correcta: " + preguntaActual.correcta, sParrafo, Color.white);
        }
        Texto(new Rect(p.x + 30, p.y + 170, p.width - 60, 150), preguntaActual.explicacion, sParrafo, new Color(1f, 1f, 0.75f));
        if (Boton(new Rect(p.x + 250, p.y + 360, 380, 70), "Continuar", Verde)) ContinuarTrasPregunta();
    }

    void UIPausa()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.55f));
        Texto(new Rect(0, 140, vw, 70), "Pausa", sCentro, Color.white);
        float x = vw / 2f - 190f;
        if (Boton(new Rect(x, 240, 380, 70), "Continuar", Verde)) { estado = Estado.Jugando; Time.timeScale = 1f; }
        if (Boton(new Rect(x, 330, 380, 70), "Reiniciar nivel", Morado)) ReintentarNivel();
        if (Boton(new Rect(x, 420, 380, 70), "Menú principal", Rosa)) IrAlMenu();
    }

    void UIFinNivel()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.6f));
        Rect p = new Rect((vw - 820f) / 2f, 130f, 820f, 460f);
        Panel(p, new Color(0.12f, 0.28f, 0.2f, 0.97f));
        Texto(new Rect(p.x, p.y + 20, p.width, 70), "¡Nivel completado!", sCentro, new Color(0.6f, 1f, 0.6f));
        Texto(new Rect(p.x + 30, p.y + 110, p.width - 60, 50), "Alimentos saludables: " + saludables + " de " + totalSaludables, sParrafo, Color.white);
        Texto(new Rect(p.x + 30, p.y + 165, p.width - 60, 50), "Vidas restantes: " + vidas + "  (+" + (vidas * BONUS_POR_VIDA) + " pts)", sParrafo, Color.white);
        Texto(new Rect(p.x + 30, p.y + 230, p.width - 60, 60), "Puntos totales: " + puntos, sCentro, new Color(1f, 0.92f, 0.3f));
        bool ultimo = nivelActual + 1 >= Niveles.Todos.Length;
        if (Boton(new Rect(p.x + 30, p.y + 340, 360, 74), ultimo ? "Ver resultado final" : "Siguiente nivel", Verde)) SiguienteNivel();
        if (Flujo.Activo && Boton(new Rect(p.x + 430, p.y + 340, 360, 74), "Volver al mundo", Morado)) Flujo.IrA(Pantalla.Mundo);
    }

    void UIGameOver()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0f, 0f, 0f, 0.65f));
        Texto(new Rect(0, 150, vw, 80), "¡Te quedaste sin vidas!", sCentro, new Color(1f, 0.55f, 0.55f));
        Texto(new Rect(0, 230, vw, 60), "Cuidado con la comida chatarra. ¡Tú puedes!", sParrafo, Color.white);
        float x = vw / 2f - 190f;
        if (Boton(new Rect(x, 330, 380, 70), "Reintentar nivel", Verde)) ReintentarNivel();
        if (Boton(new Rect(x, 420, 380, 70), "Menú principal", Rosa)) IrAlMenu();
    }

    void UIVictoria()
    {
        Panel(new Rect(0, 0, vw, VH), new Color(0.1f, 0.25f, 0.2f, 1f));
        TextoContorno(new Rect(0, 60, vw, 150), "¡Ganaste!", sTitulo, new Color(1f, 0.92f, 0.3f), new Color(0.3f, 0.2f, 0f));
        DibujarSprite(new Rect(vw / 2f - 128f, 230f, 256f, 256f), S("froxi_frente_0"));
        Texto(new Rect(0, 500, vw, 50), "Froxi llegó a salvo y comió sano. Puntos totales: " + puntos, sCentro, Color.white);
        if (Boton(new Rect(vw / 2f - 190f, 580, 380, 70), "Menú principal", Verde)) IrAlMenu();
    }
}
