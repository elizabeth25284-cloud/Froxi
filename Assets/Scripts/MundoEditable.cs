using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================================
//  MUNDO EDITABLE
//  Este componente va en el objeto "Mundo" que crea  Froxi > Mundo > Construir mapa.
//  El mapa vive en la escena (Tilemaps), asi que lo ves en la ventana Scene y
//  lo editas con la Tile Palette. Este script solo se encarga de:
//    - poner las animaciones de Froxi
//    - reiniciar a Froxi al entrar al mundo
//    - mover la camara siguiendolo
//    - mostrar el aviso de la cueva
// ============================================================================
public class MundoEditable : MonoBehaviour
{
    public JugadorMundo jugador;
    public Transform entradaCueva;
    public Tilemap suelo;          // el Tilemap que marca el tamano del mundo (Pasto)
    public Tilemap colision;       // sus casillas bloquean a Froxi; se oculta al jugar
    public Vector2 inicio = new Vector2(21.5f, 21.5f);
    public float tamanoCamara = 6.5f;
    public float velocidad = 4.5f;

    Camera cam;
    Sprite[][] anim;
    float tInicio;

    void OnEnable()
    {
        tInicio = Time.time;
        cam = Camera.main;
        if (colision != null)
        {
            TilemapRenderer tr = colision.GetComponent<TilemapRenderer>();
            if (tr != null) tr.enabled = false;
        }
        if (jugador == null) { Debug.LogError("MundoEditable: falta arrastrar a Froxi en el campo Jugador."); return; }

        if (anim == null) anim = CargarAnimacion();
        if (anim != null)
        {
            jugador.velocidad = velocidad;
            jugador.Preparar(anim);
        }
        jugador.transform.position = new Vector3(inicio.x, inicio.y, 0f);
        Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null) FisicaMundo.PonerVel(rb, Vector2.zero);

        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = tamanoCamara;
            cam.backgroundColor = new Color(0.38f, 0.72f, 0.30f);
            Seguir(true);
        }
    }

    // arma las 4 filas x 4 cuadros de Froxi a partir de la hoja ya cortada en 32x32
    static Sprite[][] CargarAnimacion()
    {
        Sprite[] todos = Resources.LoadAll<Sprite>("Arte/Personajes/froxi_hoja");
        if (todos == null || todos.Length < 16)
        {
            Debug.LogError("Froxi: corta la imagen Assets/Resources/Arte/Personajes/froxi_hoja.png en casillas de 32x32 (Sprite Editor > Slice) o usa el menu Froxi > Arte > Cortar hojas automaticamente.");
            return null;
        }
        Sprite[][] r = new Sprite[4][];
        for (int i = 0; i < 4; i++) r[i] = new Sprite[4];
        foreach (Sprite s in todos)
        {
            int col = Mathf.RoundToInt(s.rect.x / 32f);
            int fila = Mathf.RoundToInt((s.texture.height - s.rect.yMax) / 32f);
            if (fila >= 0 && fila < 4 && col >= 0 && col < 4) r[fila][col] = s;
        }
        for (int f = 0; f < 4; f++)
            for (int c = 0; c < 4; c++)
                if (r[f][c] == null) { Debug.LogError("Falta el cuadro (" + c + "," + f + ") de Froxi. La hoja debe cortarse en 4 x 4 de 32x32."); return null; }
        return r;
    }

    void LateUpdate()
    {
        if (cam != null && jugador != null) Seguir(false);
    }

    void Seguir(bool brusco)
    {
        float medioAlto = cam.orthographicSize;
        float medioAncho = medioAlto * cam.aspect;
        float minX = 0f, maxX = 40f, minY = 0f, maxY = 32f;
        if (suelo != null)
        {
            BoundsInt b = suelo.cellBounds;
            minX = b.xMin; maxX = b.xMax; minY = b.yMin; maxY = b.yMax;
        }
        float tx = Mathf.Clamp(jugador.transform.position.x, minX + medioAncho, maxX - medioAncho);
        float ty = Mathf.Clamp(jugador.transform.position.y + 0.5f, minY + medioAlto, maxY - medioAlto);
        if (maxX - minX < medioAncho * 2f) tx = (minX + maxX) / 2f;
        if (maxY - minY < medioAlto * 2f) ty = (minY + maxY) / 2f;
        Vector3 p = cam.transform.position;
        float k = brusco ? 1f : 1f - Mathf.Exp(-10f * Time.deltaTime);
        p.x = Mathf.Lerp(p.x, tx, k);
        p.y = Mathf.Lerp(p.y, ty, k);
        p.z = -10f;
        cam.transform.position = p;
    }

    void OnGUI()
    {
        if (jugador == null) return;
        if (Time.time - tInicio < 8f)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.fontSize = 22; s.fontStyle = FontStyle.Bold; s.normal.textColor = Color.white;
            GUI.Label(new Rect(16, 12, 900, 40), "Mundo de Froxi  -  Flechas o WASD para moverte", s);
        }
        if (entradaCueva != null && !Flujo.Ocupado &&
            Vector2.Distance(jugador.transform.position, entradaCueva.position) < 6f)
        {
            GUIStyle e = new GUIStyle(GUI.skin.label);
            e.fontSize = Mathf.RoundToInt(Screen.height * 0.035f);
            e.fontStyle = FontStyle.Bold; e.alignment = TextAnchor.MiddleCenter;
            e.normal.textColor = new Color(1f, 0.95f, 0.5f);
            int n = Progreso.SiguienteNivel();
            string nom = n < Niveles.Todos.Length ? Niveles.Todos[n].nombre : "";
            GUI.Label(new Rect(0, Screen.height * 0.88f, Screen.width, Screen.height * 0.08f), "Cueva  ->  Nivel " + (n + 1) + ": " + nom, e);
        }
    }
}
