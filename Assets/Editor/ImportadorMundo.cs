#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Configura SOLO los PNG de Assets/Resources/Mundo:
// 32 pixeles por unidad, sin filtro (pixel art nitido), sin compresion y legibles por codigo.
public class ImportadorMundo : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string ruta = assetPath.Replace("\\", "/");
        if (!ruta.Contains("/Resources/Mundo/")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.isReadable = true;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(s);
    }
}
#endif
