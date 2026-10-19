using UnityEngine;

// ============================================================================
//  PROGRESO: que niveles estan abiertos y cuantas estrellas lleva cada uno.
//  Se guarda solo en la computadora (PlayerPrefs). Estrellas = vidas que te sobraron (1 a 3).
// ============================================================================
public static class Progreso
{
    const string CLAVE = "froxi_desbloqueados";

    // cuantos niveles estan abiertos (el 1 siempre)
    public static int Desbloqueados { get { return Mathf.Max(1, PlayerPrefs.GetInt(CLAVE, 1)); } }

    public static bool Abierto(int nivel) { return nivel < Desbloqueados; }
    public static int Estrellas(int nivel) { return PlayerPrefs.GetInt("froxi_estrellas_" + nivel, 0); }
    public static int MejorPuntaje(int nivel) { return PlayerPrefs.GetInt("froxi_puntos_" + nivel, 0); }

    // la llama FroxiGame al terminar un nivel
    public static void Completar(int nivel, int vidasRestantes, int puntos)
    {
        int est = Mathf.Clamp(vidasRestantes, 1, 3);
        if (est > Estrellas(nivel)) PlayerPrefs.SetInt("froxi_estrellas_" + nivel, est);
        if (puntos > MejorPuntaje(nivel)) PlayerPrefs.SetInt("froxi_puntos_" + nivel, puntos);
        if (nivel + 1 > Desbloqueados) PlayerPrefs.SetInt(CLAVE, nivel + 1);
        PlayerPrefs.Save();
    }

    // a que nivel te lleva la cueva: el ultimo que tienes abierto
    public static int SiguienteNivel()
    {
        return Mathf.Clamp(Desbloqueados - 1, 0, Niveles.Todos.Length - 1);
    }

    public static void DesbloquearTodo()
    {
        PlayerPrefs.SetInt(CLAVE, Niveles.Todos.Length);
        PlayerPrefs.Save();
    }

    public static void Borrar()
    {
        PlayerPrefs.SetInt(CLAVE, 1);
        for (int i = 0; i < 50; i++)
        {
            PlayerPrefs.DeleteKey("froxi_estrellas_" + i);
            PlayerPrefs.DeleteKey("froxi_puntos_" + i);
        }
        PlayerPrefs.Save();
    }
}
