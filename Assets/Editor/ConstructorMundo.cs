#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================================
//  Menus de Froxi para el mundo:
//   Froxi > Arte > Cortar hojas automaticamente (opcional)
//        corta en 32x32 todas las hojas de Assets/Resources/Arte (si no quieres cortarlas a mano)
//   Froxi > Mundo > Construir mapa en esta escena
//        lee StreamingAssets/mundo.tmx y arma el mapa con Tilemaps dentro de la escena:
//        lo ves en Scene y lo editas con la Tile Palette.
// ============================================================================
public static class ConstructorMundo
{
    const string ARTE = "Assets/Resources/Arte";
    const string ARTE_MUNDO = "Assets/Resources/Arte/Mundo";
    const string TILES = "Assets/Tiles";
    const int T = 32;

    // ------------------------------------------------------------ cortar hojas
    [MenuItem("Froxi/Arte/Cortar hojas automaticamente (opcional)")]
    public static void CortarAuto()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ARTE });
        int cortadas = 0;
        foreach (string g in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(g);
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            if (nombre == "colision") continue;
            TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (ti == null || tex == null) continue;

            int w = tex.width, h = tex.height, cols = w / T, filas = h / T;
            Color32[] px = null;
            try { px = tex.GetPixels32(); } catch (System.Exception) { px = null; }

#pragma warning disable 618
            List<SpriteMetaData> lista = new List<SpriteMetaData>();
            for (int r = 0; r < filas; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (px != null && Vacia(px, w, c * T, h - (r + 1) * T)) continue;
                    SpriteMetaData m = new SpriteMetaData();
                    m.name = nombre + "_" + (r * cols + c);
                    m.rect = new Rect(c * T, h - (r + 1) * T, T, T);
                    if (nombre == "froxi_hoja") { m.alignment = (int)SpriteAlignment.Custom; m.pivot = new Vector2(0.5f, 0.06f); }
                    else { m.alignment = (int)SpriteAlignment.Center; m.pivot = new Vector2(0.5f, 0.5f); }
                    lista.Add(m);
                }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritesheet = lista.ToArray();
#pragma warning restore 618
            ti.SaveAndReimport();
            cortadas++;
        }
        EditorUtility.DisplayDialog("Hojas cortadas", "Se cortaron " + cortadas + " hojas en casillas de 32x32.\nAbre una en el Inspector > Sprite Editor para verlas.", "Ok");
    }

    static bool Vacia(Color32[] px, int w, int x0, int y0)
    {
        for (int y = y0; y < y0 + T; y++)
            for (int x = x0; x < x0 + T; x++)
                if (px[y * w + x].a > 0) return false;
        return true;
    }

    // ------------------------------------------------------------ construir el mapa
    class Hoja { public int primero, columnas; public string nombre; public Dictionary<int, Sprite> sprites = new Dictionary<int, Sprite>(); }
    class Capa { public string nombre; public int[] gids; }

    static readonly Dictionary<string, Tile> cache = new Dictionary<string, Tile>();

    [MenuItem("Froxi/Mundo/Construir mapa en esta escena")]
    public static void Construir()
    {
        string ruta = Path.Combine(Application.streamingAssetsPath, "mundo.tmx");
        if (!File.Exists(ruta))
        {
            string[] otros = Directory.GetFiles(Application.dataPath, "mundo.tmx", SearchOption.AllDirectories);
            if (otros.Length > 0) ruta = otros[0];
        }
        if (!File.Exists(ruta)) { EditorUtility.DisplayDialog("Falta el mapa", "No encuentro mundo.tmx en Assets/StreamingAssets.", "Ok"); return; }

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(File.ReadAllText(ruta));
        XmlElement mapa = doc.DocumentElement;
        int W = int.Parse(mapa.GetAttribute("width")), H = int.Parse(mapa.GetAttribute("height"));
        int inicioCol = 21, inicioFila = 10, cuevaCol = -1, cuevaFila = -1;
        List<Hoja> hojas = new List<Hoja>();
        List<Capa> capas = new List<Capa>();

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
                    string n = p.GetAttribute("name"), v = p.GetAttribute("value");
                    if (n == "inicio_col") int.TryParse(v, out inicioCol);
                    if (n == "inicio_fila") int.TryParse(v, out inicioFila);
                    if (n == "cueva_col") int.TryParse(v, out cuevaCol);
                    if (n == "cueva_fila") int.TryParse(v, out cuevaFila);
                }
            }
            else if (e.Name == "tileset")
            {
                Hoja h = new Hoja();
                h.primero = int.Parse(e.GetAttribute("firstgid"));
                int.TryParse(e.GetAttribute("columns"), out h.columnas);
                XmlElement img = e.SelectSingleNode("image") as XmlElement;
                h.nombre = Path.GetFileNameWithoutExtension(img != null ? img.GetAttribute("source") : e.GetAttribute("source"));
                hojas.Add(h);
            }
            else if (e.Name == "layer")
            {
                Capa c = new Capa();
                c.nombre = e.GetAttribute("name");
                XmlElement d = e.SelectSingleNode("data") as XmlElement;
                string[] partes = d.InnerText.Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                c.gids = new int[partes.Length];
                for (int i = 0; i < partes.Length; i++) c.gids[i] = (int)(uint.Parse(partes[i]) & 0x1FFFFFFF);
                capas.Add(c);
            }
        }

        // 1) leer los sprites ya cortados de cada hoja
        List<string> sinCortar = new List<string>();
        foreach (Hoja h in hojas)
        {
            string png = ARTE_MUNDO + "/" + h.nombre + ".png";
            if (!File.Exists(png)) { sinCortar.Add(h.nombre + " (no existe " + png + ")"); continue; }
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(png))
            {
                Sprite sp = o as Sprite;
                if (sp == null) continue;
                int col = Mathf.RoundToInt(sp.rect.x / T);
                int fila = Mathf.RoundToInt((sp.texture.height - sp.rect.yMax) / T);
                h.sprites[fila * Mathf.Max(1, h.columnas) + col] = sp;
            }
            if (h.sprites.Count == 0) sinCortar.Add(h.nombre);
        }
        if (sinCortar.Count > 0)
        {
            EditorUtility.DisplayDialog("Faltan hojas por cortar",
                "Primero corta estas imagenes en casillas de 32x32 (Inspector > Sprite Editor > Slice > Grid By Cell Size 32 x 32 > Apply):\n\n- " +
                string.Join("\n- ", sinCortar.ToArray()) +
                "\n\nO usa el menu Froxi > Arte > Cortar hojas automaticamente.", "Ok");
            return;
        }

        // 2) reemplazar el mundo anterior
        GameObject viejo = GameObject.Find("Mundo");
        if (viejo != null)
        {
            if (!EditorUtility.DisplayDialog("Ya hay un Mundo", "Ya existe un objeto 'Mundo' en esta escena. Se borrara y se volvera a construir (pierdes lo que hayas pintado en el).\n\n¿Continuar?", "Si", "Cancelar")) return;
            Undo.DestroyObjectImmediate(viejo);
        }
        cache.Clear();

        GameObject root = new GameObject("Mundo");
        Undo.RegisterCreatedObjectUndo(root, "Construir mapa de Froxi");
        root.AddComponent<Grid>();
        MundoEditable me = root.AddComponent<MundoEditable>();

        // 3) una Tilemap por capa
        for (int k = 0; k < capas.Count; k++)
        {
            Capa capa = capas[k];
            EditorUtility.DisplayProgressBar("Construyendo el mundo", capa.nombre, (float)k / capas.Count);
            string nombre = char.ToUpper(capa.nombre[0]) + capa.nombre.Substring(1);
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(root.transform, false);
            Tilemap tm = go.AddComponent<Tilemap>();
            TilemapRenderer tr = go.GetComponent<TilemapRenderer>();
            if (tr == null) tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = Orden(capa.nombre);

            List<Vector3Int> pos = new List<Vector3Int>();
            List<TileBase> tiles = new List<TileBase>();
            for (int r = 0; r < H; r++)
                for (int c = 0; c < W; c++)
                {
                    int gid = capa.gids[r * W + c];
                    if (gid <= 0) continue;
                    Tile t = ObtenerTile(hojas, gid);
                    if (t == null) continue;
                    pos.Add(new Vector3Int(c, H - 1 - r, 0));
                    tiles.Add(t);
                }
            tm.SetTiles(pos.ToArray(), tiles.ToArray());

            if (capa.nombre == "pasto") me.suelo = tm;
            if (capa.nombre == "colision") { go.AddComponent<TilemapCollider2D>(); me.colision = tm; }
        }
        EditorUtility.ClearProgressBar();

        // 4) paredes en los bordes del mapa
        GameObject bordes = new GameObject("Bordes");
        bordes.transform.SetParent(root.transform, false);
        Caja(bordes, new Vector2(-0.5f, H / 2f), new Vector2(1f, H + 4f));
        Caja(bordes, new Vector2(W + 0.5f, H / 2f), new Vector2(1f, H + 4f));
        Caja(bordes, new Vector2(W / 2f, -0.5f), new Vector2(W + 4f, 1f));
        Caja(bordes, new Vector2(W / 2f, H + 0.5f), new Vector2(W + 4f, 1f));

        // 5) Froxi
        Sprite frente = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ARTE + "/Personajes/froxi_hoja.png"))
        {
            Sprite sp = o as Sprite;
            if (sp == null) continue;
            if (Mathf.RoundToInt(sp.rect.x / T) == 0 && Mathf.RoundToInt((sp.texture.height - sp.rect.yMax) / T) == 3) frente = sp;
        }
        float pivoteY = frente != null ? frente.pivot.y / T : 0.06f;
        GameObject f = new GameObject("Froxi");
        f.transform.SetParent(root.transform, false);
        Vector2 inicio = new Vector2(inicioCol + 0.5f, H - inicioFila - 0.5f + pivoteY);
        f.transform.position = new Vector3(inicio.x, inicio.y, 0f);
        SpriteRenderer sr = f.AddComponent<SpriteRenderer>();
        sr.sprite = frente;
        sr.sortingOrder = 0;
        Rigidbody2D rb = f.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D cc = f.AddComponent<CapsuleCollider2D>();
        cc.direction = CapsuleDirection2D.Horizontal;
        cc.size = new Vector2(0.5f, 0.3f);
        cc.offset = new Vector2(0f, 0.1f - pivoteY);
        PhysicsMaterial2D pm = new PhysicsMaterial2D("sinFriccion");
        pm.friction = 0f; pm.bounciness = 0f;
        cc.sharedMaterial = pm;
        JugadorMundo jm = f.AddComponent<JugadorMundo>();
        me.jugador = jm;
        me.inicio = inicio;

        // 6) la entrada de la cueva (zona invisible + flecha)
        if (cuevaCol >= 0)
        {
            GameObject z = new GameObject("EntradaCueva");
            z.transform.SetParent(root.transform, false);
            z.transform.position = new Vector3(cuevaCol + 0.5f, H - cuevaFila - 0.1f, 0f);
            BoxCollider2D bc = z.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.2f, 1.0f);
            z.AddComponent<EntradaCueva>();
            me.entradaCueva = z.transform;

            Sprite fl = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/ui_flecha_arriba.png");
            GameObject fg = new GameObject("FlechaCueva");
            fg.transform.SetParent(root.transform, false);
            fg.transform.position = new Vector3(cuevaCol + 0.5f, H - cuevaFila - 1.2f, 0f);
            SpriteRenderer fs = fg.AddComponent<SpriteRenderer>();
            fs.sprite = fl;
            fs.sortingOrder = -5;
            fg.AddComponent<FlechaRebote>();
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        EditorUtility.DisplayDialog("Mapa construido",
            "Listo: en Hierarchy tienes el objeto 'Mundo' con una Tilemap por capa, Froxi y la entrada de la cueva.\n\nGuarda la escena con Ctrl+S y presiona Play.", "Ok");
    }

    static int Orden(string capa)
    {
        switch (capa)
        {
            case "pasto": return -100;
            case "cultivo": return -99;
            case "camino": return -98;
            case "arena": return -97;
            case "agua": return -96;
            case "oceano": return -95;
            case "objetos_bajo": return -10;
            case "objetos_encima": return 10;
            case "colision": return 20;
        }
        return 0;
    }

    static void Caja(GameObject go, Vector2 centro, Vector2 tam)
    {
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.offset = centro;
        bc.size = tam;
    }

    // convierte un numero de pieza del mapa (gid) en un Tile guardado en Assets/Tiles
    static Tile ObtenerTile(List<Hoja> hojas, int gid)
    {
        Hoja h = null;
        foreach (Hoja x in hojas) if (gid >= x.primero && (h == null || x.primero > h.primero)) h = x;
        if (h == null) return null;
        int id = gid - h.primero;
        string clave = h.nombre + "_" + id;
        Tile t;
        if (cache.TryGetValue(clave, out t)) return t;
        Sprite sp;
        if (!h.sprites.TryGetValue(id, out sp)) { cache[clave] = null; return null; }

        string carpeta = TILES + "/" + h.nombre;
        if (!AssetDatabase.IsValidFolder(TILES)) AssetDatabase.CreateFolder("Assets", "Tiles");
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder(TILES, h.nombre);
        string ruta = carpeta + "/" + clave + ".asset";
        t = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(t, ruta);
        }
        t.sprite = sp;
        t.colliderType = h.nombre == "colision" ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        EditorUtility.SetDirty(t);
        cache[clave] = t;
        return t;
    }
}
#endif
