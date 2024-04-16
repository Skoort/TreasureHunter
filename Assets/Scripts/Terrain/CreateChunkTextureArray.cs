using UnityEngine;

public class CreateChunkTextureArray : MonoBehaviour
{
    [SerializeField]
    private Texture2D[] _textures = default;

    [SerializeField]
    private Material _chunkMaterial = default;

    private void Start()
    {
        var textureArray = new Texture2DArray(
            _textures[0].width,
            _textures[0].height,
            _textures.Length,
            TextureFormat.RGBA32,
            false,
            false
        );
        textureArray.filterMode = FilterMode.Point;
        textureArray.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < _textures.Length; ++i)
        {
            textureArray.SetPixels(
                _textures[i].GetPixels(),
                i
            );
        }
        textureArray.Apply();

        _chunkMaterial.SetTexture("_MainTexArray", textureArray);
    }
}
