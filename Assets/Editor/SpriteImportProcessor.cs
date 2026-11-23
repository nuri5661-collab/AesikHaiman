using UnityEditor;
using UnityEngine;

namespace AesikHaiman.EditorTools
{
    /// <summary>
    /// Assets/Sprites 폴더 아래로 들어오는 모든 이미지를 자동으로 Sprite(2D and UI) 타입으로 임포트합니다.
    /// 이게 없으면 기본 Texture 타입으로 들어와서 AnimationClipBuilder가 스프라이트로 인식하지 못합니다.
    /// </summary>
    public class SpriteImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("Assets/Sprites/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;

            // 바닥처럼 SpriteRenderer.drawMode = Tiled로 길게 반복시키는 텍스처는
            // FullRect 메시 + Repeat 랩모드가 아니면 이음새가 깨지거나 늘어나 보임.
            // (spriteMeshType은 TextureImporter에 직접 있는 속성이 아니라 TextureImporterSettings를 통해서만 설정 가능)
            if (path.Contains("Assets/Sprites/Ground/"))
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);

                importer.wrapMode = TextureWrapMode.Repeat;
            }
        }
    }
}
