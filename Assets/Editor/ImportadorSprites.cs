#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Configura SOLO los PNG que estan en Assets/Resources/Sprites:
// 32 pixeles por unidad, sin filtro (pixel art nitido) y sin compresion.
public class ImportadorSprites : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string ruta = assetPath.Replace("\\", "/");
        if (!ruta.Contains("/Resources/Sprites/")) return;

        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(s);
    }
}
#endif
