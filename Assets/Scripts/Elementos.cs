using UnityEngine;

// Ayuda para que el codigo funcione en todas las versiones de Unity (2020 a Unity 6)
public static class Fisica
{
    public static Vector2 Vel(Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }
    public static void PonerVel(Rigidbody2D rb, Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}

// Cambia los cuadros de un sprite en bucle (animacion sin Animator)
public class AnimacionSprite : MonoBehaviour
{
    public Sprite[] cuadros;
    public float fps = 6f;
    SpriteRenderer sr;
    float desfase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        desfase = Random.value * 4f;
    }

    void Update()
    {
        if (cuadros == null || cuadros.Length == 0 || sr == null) return;
        int i = (int)(Time.time * fps + desfase) % cuadros.Length;
        sr.sprite = cuadros[i];
    }
}

// ----------------------------------------------------------------------
// FROXI (jugador)
// ----------------------------------------------------------------------
public class JugadorFroxi : MonoBehaviour
{
    public float velocidad = 7f;
    public float fuerzaSalto = 14.5f;
    public Sprite[] caminar;            // 4 cuadros mirando a la derecha
    public Rigidbody2D rb;
    public BoxCollider2D col;
    public bool invulnerable;
    public Vector3 ultimaSegura;

    SpriteRenderer sr;
    float finInvulnerable;
    float ultimoSuelo = -10f;
    float ultimoSaltoPedido = -10f;
    float h;
    bool mirandoIzquierda;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    bool EnSuelo { get { return Time.time - ultimoSuelo < 0.1f; } }

    static bool PidioSaltar()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.X) ||
               Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
    }
    static bool SoltoSalto()
    {
        return Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.X) ||
               Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W);
    }

    void Update()
    {
        if (FroxiGame.I == null || FroxiGame.I.estado != Estado.Jugando) return;

        h = Input.GetAxisRaw("Horizontal");
        if (h < -0.1f) mirandoIzquierda = true;
        if (h > 0.1f) mirandoIzquierda = false;
        sr.flipX = mirandoIzquierda;

        if (PidioSaltar()) ultimoSaltoPedido = Time.time;

        // salto (con un pequeno margen para que se sienta bien)
        if (Time.time - ultimoSaltoPedido < 0.12f && EnSuelo)
        {
            Vector2 v = Fisica.Vel(rb);
            Fisica.PonerVel(rb, new Vector2(v.x, fuerzaSalto));
            ultimoSaltoPedido = -10f;
            ultimoSuelo = -10f;
        }
        // soltar la tecla = salto mas corto
        if (SoltoSalto())
        {
            Vector2 v = Fisica.Vel(rb);
            if (v.y > 0f) Fisica.PonerVel(rb, new Vector2(v.x, v.y * 0.5f));
        }

        // animacion
        if (caminar != null && caminar.Length >= 4)
        {
            if (!EnSuelo) sr.sprite = caminar[2];
            else if (Mathf.Abs(h) > 0.1f) sr.sprite = caminar[(int)(Time.time * 10f) % 4];
            else sr.sprite = caminar[1];
        }

        // parpadeo mientras es invulnerable
        if (invulnerable)
        {
            if (Time.time > finInvulnerable) { invulnerable = false; sr.color = Color.white; }
            else sr.color = new Color(1f, 1f, 1f, Mathf.Sin(Time.time * 35f) > 0f ? 1f : 0.3f);
        }
    }

    void FixedUpdate()
    {
        if (FroxiGame.I == null || FroxiGame.I.estado != Estado.Jugando) return;

        Vector2 v = Fisica.Vel(rb);
        float vy = Mathf.Max(v.y, -22f);
        Fisica.PonerVel(rb, new Vector2(h * velocidad, vy));

        // guardar el ultimo lugar seguro (por si se cae a un hueco)
        if (EnSuelo)
        {
            Vector2 p = transform.position;
            bool izq = Physics2D.Raycast(p + Vector2.left * 0.4f, Vector2.down, 1.0f).collider != null;
            bool der = Physics2D.Raycast(p + Vector2.right * 0.4f, Vector2.down, 1.0f).collider != null;
            if (izq && der) ultimaSegura = transform.position;
        }

        // se cayo al vacio: regresa al ultimo lugar seguro (no pierde vida)
        if (transform.position.y < -4f)
        {
            transform.position = ultimaSegura + Vector3.up * 0.3f;
            Fisica.PonerVel(rb, Vector2.zero);
        }
    }

    void OnCollisionEnter2D(Collision2D c) { RevisarSuelo(c); }
    void OnCollisionStay2D(Collision2D c) { RevisarSuelo(c); }
    void RevisarSuelo(Collision2D c)
    {
        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.GetContact(i).normal.y > 0.5f) { ultimoSuelo = Time.time; return; }
        }
    }

    public void Invulnerabilidad(float segundos)
    {
        invulnerable = true;
        finInvulnerable = Time.time + segundos;
    }

    public void Rebotar()
    {
        Vector2 v = Fisica.Vel(rb);
        Fisica.PonerVel(rb, new Vector2(v.x, fuerzaSalto * 0.7f));
    }
}

// ----------------------------------------------------------------------
// COMIDA CHATARRA (enemigo)
// ----------------------------------------------------------------------
public class ComidaChatarra : MonoBehaviour
{
    public float velocidad = 1.6f;
    public bool patrulla = true;
    public Rigidbody2D rb;
    public BoxCollider2D col;

    int dir = -1;
    float proximoGiro;
    bool congelada;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        dir = Random.value < 0.5f ? -1 : 1;
    }

    public void Congelar() { congelada = true; }

    void FixedUpdate()
    {
        if (congelada || FroxiGame.I == null || FroxiGame.I.estado != Estado.Jugando) return;
        Vector2 v = Fisica.Vel(rb);
        if (!patrulla) { Fisica.PonerVel(rb, new Vector2(0f, v.y)); return; }

        Vector2 p = transform.position;
        bool pared = Physics2D.Raycast(p + Vector2.up * 0.1f, Vector2.right * dir, 0.55f).collider != null;
        bool hayPiso = Physics2D.Raycast(p + Vector2.right * dir * 0.5f, Vector2.down, 0.9f).collider != null;
        if ((pared || !hayPiso) && Time.time > proximoGiro)
        {
            dir = -dir;
            proximoGiro = Time.time + 0.3f;
        }
        Fisica.PonerVel(rb, new Vector2(dir * velocidad, v.y));
    }

    void Update()
    {
        if (congelada || FroxiGame.I == null || FroxiGame.I.estado != Estado.Jugando) return;
        JugadorFroxi pl = FroxiGame.I.jugador;
        if (pl == null) return;
        if (pl.col.bounds.Intersects(col.bounds))
        {
            bool pisada = pl.transform.position.y - transform.position.y > 0.4f &&
                          Fisica.Vel(pl.rb).y <= 0.5f;
            FroxiGame.I.ContactoChatarra(this, pisada);
        }
    }
}

// ----------------------------------------------------------------------
// COMIDA SALUDABLE (se recoge para ganar puntos)
// ----------------------------------------------------------------------
public class ComidaSaludable : MonoBehaviour
{
    Vector3 basePos;
    float fase;

    void Start()
    {
        basePos = transform.position;
        fase = Random.value * 6.28f;
    }

    void Update()
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3f + fase) * 0.08f;
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (FroxiGame.I != null && o.GetComponent<JugadorFroxi>() != null)
            FroxiGame.I.RecogerSaludable(this);
    }
}

// ----------------------------------------------------------------------
// META (casa / bandera)
// ----------------------------------------------------------------------
public class MetaNivel : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D o)
    {
        if (FroxiGame.I != null && o.GetComponent<JugadorFroxi>() != null)
            FroxiGame.I.TerminarNivel();
    }
}
