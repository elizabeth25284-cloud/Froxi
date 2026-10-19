using UnityEngine;

// ============================================================================
//  ESCENA FROXI
//  Ponlo en un objeto vacio de la escena para decir QUE pantalla o nivel abre
//  esa escena al darle Play. Asi cada nivel tiene su propia escena y se puede
//  probar solo (abres la escena, Play).
//  Si una escena no lo tiene, el juego empieza en la portada.
// ============================================================================
public class EscenaFroxi : MonoBehaviour
{
    public Pantalla pantalla = Pantalla.Portada;

    [Tooltip("Solo para Pantalla = Plataforma. 0 = Nivel 1, 1 = Nivel 2, 2 = Nivel 3 ...")]
    public int nivel = 0;
}
