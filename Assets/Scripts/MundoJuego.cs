using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

// ============================================================================
//  FROXI - MUNDO VISTA DESDE ARRIBA
//  Lee el mapa de Tiled (Assets/StreamingAssets/mundo.tmx), arma el suelo y los
//  objetos con las piezas de 32x32, pone los choques y a Froxi.
//  No tienes que poner nada en la escena: se crea solo al darle Play.
// ============================================================================

public static class FisicaMundo
{
    public static void PonerVel(Rigidbody2D rb, Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}

public class MundoJuego : MonoBehaviour
{
    // ============ AJUSTES FACILES ============
    public const bool AUTO_INICIAR = false;      // en el kit completo lo crea "Flujo"; true = probar solo el mundo
    public const string ARCHIVO_MAPA = "mundo.tmx";
    public const float TAMANO_CAMARA = 6.5f;     // mas grande = se ve mas mundo
    public const float VELOCIDAD_FROXI = 4.5f;
    // =========================================

    const int TILE = 32;
    public static MundoJuego I;

    class Tileset { public int first; public string nombre; public Texture2D tex; public int cols; public Color32[] px; }
    class Capa { public string nombre; public int[] gids; }

    int W, H, inicioCol = 21, inicioFila = 10;
    int cuevaCol = -1, cuevaFila = -1;
    readonly List<Object> creadas = new List<Object>();
    readonly List<Tileset> tilesets = new List<Tileset>();
    readonly List<Capa> capas = new List<Capa>();
    Camera cam;
    JugadorMundo jugador;
    Material matSprite;
    float tInicio;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Auto()
    {
        if (AUTO_INICIAR && I == null) new GameObject("MundoJuego").AddComponent<MundoJuego>();
    }

    void Awake()
    {
        I = this;
        tInicio = Time.time;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh != null) matSprite = new Material(sh);

        cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera");
            g.tag = "MainCamera";
            cam = g.AddComponent<Camera>();
            g.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = TAMANO_CAMARA;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.38f, 0.72f, 0.30f);

        if (!LeerMapa()) return;
        ConstruirCapas();
        ConstruirColisiones();
        CrearJugador();
        CrearEntradaCueva();
    }

    void OnDestroy()
    {
        foreach (Object o in creadas) if (o != null) Destroy(o);
        creadas.Clear();
        if (I == this) I = null;
    }

    // ------------------------------------------------------------ la cueva = puerta al siguiente nivel
    void CrearEntradaCueva()
    {
        if (cuevaCol < 0) { Debug.LogWarning("El mapa no tiene cueva_col / cueva_fila: no hay entrada a la cueva."); return; }

        // zona invisible justo en la boca de la cueva (un poco hacia arriba para que Froxi la toque al chocar)
        GameObject z = new GameObject("EntradaCueva");
        z.transform.SetParent(transform);
        z.transform.position = new Vector3(cuevaCol + 0.5f, H - cuevaFila - 0.1f, 0f);
        BoxCollider2D bc = z.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.2f, 1.0f);
        z.AddComponent<EntradaCueva>();

        // flecha que sube y baja para que se note por donde entrar
        Sprite fl = Resources.Load<Sprite>("UI/ui_flecha_arriba");
        if (fl != null)
        {
            GameObject f = new GameObject("FlechaCueva");
            f.transform.SetParent(transform);
            f.transform.position = new Vector3(cuevaCol + 0.5f, H - cuevaFila - 1.2f, 0f);
            SpriteRenderer sr = f.AddComponent<SpriteRenderer>();
            sr.sprite = fl;
            sr.sortingOrder = -5;
            if (matSprite != null) sr.sharedMaterial = matSprite;
            f.AddComponent<FlechaRebote>();
        }
    }

    // ------------------------------------------------------------ leer el .tmx
    bool LeerMapa()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, ARCHIVO_MAPA);
        if (!File.Exists(ruta))
        {
            // por si la carpeta quedo en otro lugar (por ejemplo Assets/Assets/StreamingAssets): la busca
            try
            {
                string[] otros = Directory.GetFiles(Application.dataPath, ARCHIVO_MAPA, SearchOption.AllDirectories);
                if (otros.Length > 0) ruta = otros[0];
            }
            catch (System.Exception) { }
        }
        if (!File.Exists(ruta))
        {
            Debug.LogError("No encuentro " + ruta + "  (copia mundo.tmx a Assets/StreamingAssets)");
            return false;
        }
        XmlDocument doc = new XmlDocument();
        try { doc.LoadXml(File.ReadAllText(ruta)); }
        catch (System.Exception e) { Debug.LogError("No pude leer el mapa: " + e.Message); return false; }

        XmlElement mapa = doc.DocumentElement;
        W = int.Parse(mapa.GetAttribute("width"));
        H = int.Parse(mapa.GetAttribute("height"));

        foreach (XmlNode nodo in mapa.ChildNodes)
        {
            XmlElement e = nodo as XmlElement;
            if (e == null) continue;

            if (e.Name == "properties")
            {
                foreach (XmlNode pn in e.ChildNodes)
                {
                    XmlElement p = pn as XmlElement;
                    if (p == null) continue;
                    if (p.GetAttribute("name") == "inicio_col") int.TryParse(p.GetAttribute("value"), out inicioCol);
                    if (p.GetAttribute("name") == "inicio_fila") int.TryParse(p.GetAttribute("value"), out inicioFila);
                    if (p.GetAttribute("name") == "cueva_col") int.TryParse(p.GetAttribute("value"), out cuevaCol);
                    if (p.GetAttribute("name") == "cueva_fila") int.TryParse(p.GetAttribute("value"), out cuevaFila);
                }
            }
            else if (e.Name == "tileset")
            {
                Tileset t = new Tileset();
                t.first = int.Parse(e.GetAttribute("firstgid"));
                string origen = e.GetAttribute("source");
                XmlElement img = e.SelectSingleNode("image") as XmlElement;
                if (img != null) origen = img.GetAttribute("source");
                t.nombre = Path.GetFileNameWithoutExtension(origen);
                Sprite sp = Resources.Load<Sprite>("Mundo/tiles/" + t.nombre);
                if (sp == null) { Debug.LogError("Falta la imagen Assets/Resources/Mundo/tiles/" + t.nombre + ".png"); continue; }
                t.tex = sp.texture;
                t.cols = t.tex.width / TILE;
                try { t.px = t.tex.GetPixels32(); }
                catch (System.Exception) { Debug.LogError("La imagen " + t.nombre + " no es legible. Selecciona la carpeta Resources/Mundo, clic derecho > Reimport."); continue; }
                tilesets.Add(t);
            }
            else if (e.Name == "layer")
            {
                XmlElement d = e.SelectSingleNode("data") as XmlElement;
                if (d == null) continue;
                if (d.GetAttribute("encoding") != "csv")
                {
                    Debug.LogError("La capa '" + e.GetAttribute("name") + "' no esta en CSV. En Tiled: Mapa > Propiedades > Formato de capa de tiles = CSV.");
                    continue;
                }
                string[] partes = d.InnerText.Split(new char[] { ',', '\n', '\r', ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                Capa c = new Capa();
                c.nombre = e.GetAttribute("name");
                c.gids = new int[W * H];
                for (int i = 0; i < partes.Length && i < c.gids.Length; i++)
                {
                    long v = long.Parse(partes[i]);
                    c.gids[i] = (int)(v & 0x1FFFFFFF);   // se ignoran los giros de Tiled
                }
                capas.Add(c);
            }
        }
        return true;
    }

    // ------------------------------------------------------------ dibujar las capas
    Tileset BuscarTileset(int gid)
    {
        Tileset mejor = null;
        for (int i = 0; i < tilesets.Count; i++)
            if (tilesets[i].first <= gid && (mejor == null || tilesets[i].first > mejor.first)) mejor = tilesets[i];
        return mejor;
    }

    void PegarTile(Color32[] destino, int destW, int c, int r, int gid)
    {
        Tileset t = BuscarTileset(gid);
        if (t == null) return;
        int lid = gid - t.first;
        int sx = (lid % t.cols) * TILE;
        int sy = t.tex.height - ((lid / t.cols) + 1) * TILE;
        if (sy < 0) return;
        int dx = c * TILE;
        int dy = (H - 1 - r) * TILE;
        for (int y = 0; y < TILE; y++)
        {
            int si = (sy + y) * t.tex.width + sx;
            int di = (dy + y) * destW + dx;
            for (int x = 0; x < TILE; x++)
            {
                Color32 s = t.px[si + x];
                if (s.a == 0) continue;
                if (s.a == 255) { destino[di + x] = s; continue; }
                Color32 d = destino[di + x];
                float sa = s.a / 255f, da = d.a / 255f;
                float oa = sa + da * (1f - sa);
                destino[di + x] = new Color32(
                    (byte)((s.r * sa + d.r * da * (1f - sa)) / oa),
                    (byte)((s.g * sa + d.g * da * (1f - sa)) / oa),
                    (byte)((s.b * sa + d.b * da * (1f - sa)) / oa),
                    (byte)(oa * 255f));
            }
        }
    }

    void ConstruirCapas()
    {
        int pw = W * TILE, ph = H * TILE;
        Color32[] suelo = new Color32[pw * ph];
        Color32[] bajo = new Color32[pw * ph];
        Color32[] encima = new Color32[pw * ph];
        bool hayBajo = false, hayEncima = false;

        for (int k = 0; k < capas.Count; k++)
        {
            string n = capas[k].nombre.ToLower();
            if (n == "colision" || n.StartsWith("hide_")) continue;
            Color32[] destino = suelo;
            if (n.EndsWith("encima")) { destino = encima; hayEncima = true; }
            else if (n.StartsWith("objetos")) { destino = bajo; hayBajo = true; }
            int[] gids = capas[k].gids;
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                    if (gids[r * W + c] > 0) PegarTile(destino, pw, c, r, gids[r * W + c]);
        }

        CrearCapaVisual("Suelo", suelo, pw, ph, -100);
        if (hayBajo) CrearCapaVisual("ObjetosBajo", bajo, pw, ph, -10);
        if (hayEncima) CrearCapaVisual("ObjetosEncima", encima, pw, ph, 10);
    }

    void CrearCapaVisual(string nombre, Color32[] px, int pw, int ph, int orden)
    {
        Texture2D t = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels32(px);
        t.Apply();
        creadas.Add(t);
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(t, new Rect(0, 0, pw, ph), Vector2.zero, TILE, 0, SpriteMeshType.FullRect);
        creadas.Add(sr.sprite);
        sr.sortingOrder = orden;
        if (matSprite != null) sr.sharedMaterial = matSprite;
    }

    // ------------------------------------------------------------ choques
    void ConstruirColisiones()
    {
        bool[,] bloq = new bool[H, W];
        bool[,] usado = new bool[H, W];
        for (int k = 0; k < capas.Count; k++)
        {
            if (capas[k].nombre.ToLower() != "colision") continue;
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                    if (capas[k].gids[r * W + c] > 0) bloq[r, c] = true;
        }

        GameObject go = new GameObject("Colisiones");
        go.transform.SetParent(transform);

        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                if (!bloq[r, c] || usado[r, c]) continue;
                int ancho = 1;
                while (c + ancho < W && bloq[r, c + ancho] && !usado[r, c + ancho]) ancho++;
                int alto = 1;
                bool sigue = true;
                while (r + alto < H && sigue)
                {
                    for (int i = 0; i < ancho; i++)
                        if (!bloq[r + alto, c + i] || usado[r + alto, c + i]) { sigue = false; break; }
                    if (sigue) alto++;
                }
                for (int j = 0; j < alto; j++)
                    for (int i = 0; i < ancho; i++) usado[r + j, c + i] = true;
                BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
                bc.offset = new Vector2(c + ancho / 2f, H - r - alto / 2f);
                bc.size = new Vector2(ancho, alto);
            }

        // paredes invisibles en los bordes del mapa
        GameObject borde = new GameObject("BordesDelMapa");
        borde.transform.SetParent(transform);
        AgregarCaja(borde, new Vector2(-0.5f, H / 2f), new Vector2(1f, H + 4f));
        AgregarCaja(borde, new Vector2(W + 0.5f, H / 2f), new Vector2(1f, H + 4f));
        AgregarCaja(borde, new Vector2(W / 2f, -0.5f), new Vector2(W + 4f, 1f));
        AgregarCaja(borde, new Vector2(W / 2f, H + 0.5f), new Vector2(W + 4f, 1f));
    }

    void AgregarCaja(GameObject go, Vector2 centro, Vector2 tam)
    {
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.offset = centro;
        bc.size = tam;
    }

    // ------------------------------------------------------------ Froxi
    void CrearJugador()
    {
        Sprite hoja = Resources.Load<Sprite>("Mundo/froxi_hoja");
        if (hoja == null) { Debug.LogError("Falta Assets/Resources/Mundo/froxi_hoja.png"); return; }
        Texture2D tex = hoja.texture;

        // filas de la hoja: 0 derecha, 1 izquierda, 2 arriba (espalda), 3 abajo (frente)
        Sprite[][] anim = new Sprite[4][];
        for (int fila = 0; fila < 4; fila++)
        {
            anim[fila] = new Sprite[4];
            for (int f = 0; f < 4; f++)
                anim[fila][f] = Sprite.Create(tex, new Rect(f * TILE, tex.height - (fila + 1) * TILE, TILE, TILE),
                                              new Vector2(0.5f, 0.06f), TILE, 0, SpriteMeshType.FullRect);
        }

        GameObject go = new GameObject("Froxi");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(inicioCol + 0.5f, H - inicioFila - 0.5f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;
        if (matSprite != null) sr.sharedMaterial = matSprite;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.5f, 0.3f);
        bc.offset = new Vector2(0f, 0.1f);
        PhysicsMaterial2D mat = new PhysicsMaterial2D("sinFriccion");
        mat.friction = 0f;
        mat.bounciness = 0f;
        bc.sharedMaterial = mat;

        jugador = go.AddComponent<JugadorMundo>();
        jugador.velocidad = VELOCIDAD_FROXI;
        jugador.Preparar(anim);
        PosicionarCamara(true);
    }

    // ------------------------------------------------------------ camara
    void LateUpdate()
    {
        if (jugador != null) PosicionarCamara(false);
    }

    void PosicionarCamara(bool brusco)
    {
        float medioAlto = cam.orthographicSize;
        float medioAncho = medioAlto * cam.aspect;
        float tx = Mathf.Clamp(jugador.transform.position.x, medioAncho, W - medioAncho);
        float ty = Mathf.Clamp(jugador.transform.position.y + 0.5f, medioAlto, H - medioAlto);
        if (W < medioAncho * 2f) tx = W / 2f;
        if (H < medioAlto * 2f) ty = H / 2f;
        Vector3 p = cam.transform.position;
        float k = brusco ? 1f : 1f - Mathf.Exp(-10f * Time.deltaTime);
        p.x = Mathf.Lerp(p.x, tx, k);
        p.y = Mathf.Lerp(p.y, ty, k);
        p.z = -10f;
        cam.transform.position = p;
    }

    void OnGUI()
    {
        if (jugador != null && cuevaCol >= 0 && !Flujo.Ocupado)
        {
            Vector2 c = new Vector2(cuevaCol + 0.5f, H - cuevaFila);
            if (Vector2.Distance(jugador.transform.position, c) < 6f)
            {
                GUIStyle e = new GUIStyle(GUI.skin.label);
                e.fontSize = Mathf.RoundToInt(Screen.height * 0.035f);
                e.fontStyle = FontStyle.Bold;
                e.alignment = TextAnchor.MiddleCenter;
                e.normal.textColor = new Color(1f, 0.95f, 0.5f);
                int n = Progreso.SiguienteNivel();
                string nom = n < Niveles.Todos.Length ? Niveles.Todos[n].nombre : "";
                GUI.Label(new Rect(0, Screen.height * 0.88f, Screen.width, Screen.height * 0.08f), "Cueva  →  Nivel " + (n + 1) + ": " + nom, e);
            }
        }
        if (Time.time - tInicio > 8f) return;
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = 22;
        s.fontStyle = FontStyle.Bold;
        s.normal.textColor = Color.white;
        GUI.Label(new Rect(16, 12, 900, 40), "Mundo de Froxi  -  Flechas o WASD para moverte", s);
    }
}

// ----------------------------------------------------------------------------
//  FROXI (vista desde arriba): camina en 4 direcciones
// ----------------------------------------------------------------------------
public class JugadorMundo : MonoBehaviour
{
    public float velocidad = 4.5f;
    Sprite[][] anim;
    SpriteRenderer sr;
    Rigidbody2D rb;
    int dir = 3;
    Vector2 mov;

    void OnEnable() { mov = Vector2.zero; }

    public void Preparar(Sprite[][] a)
    {
        anim = a;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        sr.sprite = anim[dir][0];
    }

    void Update()
    {
        if (anim == null) return;
        float x = Flujo.Ocupado ? 0f : Input.GetAxisRaw("Horizontal");
        float y = Flujo.Ocupado ? 0f : Input.GetAxisRaw("Vertical");
        mov = new Vector2(x, y);
        if (mov.sqrMagnitude > 1f) mov.Normalize();

        if (Mathf.Abs(x) > Mathf.Abs(y)) dir = x > 0f ? 0 : 1;
        else if (Mathf.Abs(y) > 0f) dir = y > 0f ? 2 : 3;

        int cuadro = mov.sqrMagnitude > 0.01f ? (int)(Time.time * 9f) % 4 : 0;
        sr.sprite = anim[dir][cuadro];
    }

    void FixedUpdate()
    {
        if (rb != null) FisicaMundo.PonerVel(rb, mov * velocidad);
    }
}


// ----------------------------------------------------------------------------
//  Entrada de la cueva: cuando Froxi la toca, se pasa al siguiente nivel
// ----------------------------------------------------------------------------
public class EntradaCueva : MonoBehaviour
{
    bool usada;

    void OnEnable() { usada = false; }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (usada || otro.GetComponent<JugadorMundo>() == null) return;
        usada = true;
        Flujo.EntrarCueva();
    }
}

// Mueve un sprite hacia arriba y abajo (la flecha de la cueva)
public class FlechaRebote : MonoBehaviour
{
    Vector3 baseP;
    void Start() { baseP = transform.position; }
    void Update() { transform.position = baseP + new Vector3(0f, Mathf.Sin(Time.time * 4f) * 0.12f, 0f); }
}
