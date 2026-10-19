#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Configura los PNG de Assets/Resources/Arte (las hojas que tu cortas en el Sprite Editor):
// 32 pixeles por unidad, sin suavizado (pixel art nitido), sin compresion.
// Las hojas quedan en "Multiple" para que las cortes en casillas de 32x32.
// La imagen "colision" es una sola casilla, asi que va en "Single".
public class ImportadorArte : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string ruta = assetPath.Replace("\\", "/");
        if (!ruta.Contains("/Resources/Arte/")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
        bool unaSola = nombre == "colision";

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = unaSola ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 32;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.isReadable = true;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
#endif
