#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu:  Froxi > Crear escenas del juego
// Crea (o reemplaza) en Assets/Scenes:
//   Principal  = portada + (despues) el mundo editable
//   Nivel_01, Nivel_02 ...  = una escena por nivel de plataformas, para probarlos solos
// y las agrega a Build Settings.
public static class CrearEscenas
{
    [MenuItem("Froxi/Crear escenas del juego")]
    public static void Crear()
    {
        if (!EditorUtility.DisplayDialog("Crear escenas",
            "Se crearan las escenas Principal y una por nivel en Assets/Scenes.\nLas que ya existan con ese nombre se reemplazan.\n\nSi tienes cambios sin guardar en la escena actual, se guardaran primero.\n\n¿Continuar?",
            "Si", "Cancelar")) return;
        if (SceneManager.GetActiveScene().isDirty)
        {
            if (!EditorSceneManager.SaveOpenScenes()) return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        List<EditorBuildSettingsScene> lista = new List<EditorBuildSettingsScene>();

        lista.Add(Hacer("Principal", Pantalla.Portada, 0));
        for (int i = 0; i < Niveles.Todos.Length; i++)
            lista.Add(Hacer("Nivel_" + (i + 1).ToString("00"), Pantalla.Plataforma, i));

        EditorBuildSettings.scenes = lista.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity");
        EditorUtility.DisplayDialog("Listo",
            "Escenas creadas en Assets/Scenes.\nSe abrio 'Principal'. Siguiente paso: menu Froxi > Mundo > Construir mapa en esta escena.", "Ok");
    }

    static EditorBuildSettingsScene Hacer(string nombre, Pantalla pantalla, int nivel)
    {
        Scene esc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject go = new GameObject("Escena Froxi (" + nombre + ")");
        EscenaFroxi ef = go.AddComponent<EscenaFroxi>();
        ef.pantalla = pantalla;
        ef.nivel = nivel;
        string ruta = "Assets/Scenes/" + nombre + ".unity";
        EditorSceneManager.SaveScene(esc, ruta);
        return new EditorBuildSettingsScene(ruta, true);
    }
}
#endif
