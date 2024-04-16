using UnityEngine;

public static class VectorExtensions
{
    public static Vector3Int WorldToLocal(this Vector3 position, Chunk localTo)
    {
        return WorldToLocal(
            new Vector3Int(
                Mathf.FloorToInt(position.x),
                Mathf.FloorToInt(position.y),
                0
            ),
            localTo
        );
    }

    public static Vector3Int WorldToLocal(this Vector3Int position, Chunk localTo)
    {
        return new Vector3Int(
            position.x - localTo.Id.x * localTo.Size.x,
            position.y - localTo.Id.y * localTo.Size.y,
            0
        );
    }

    public static Vector2Int WorldToLocal(this Vector2 position, Chunk localTo)
    {
        return (Vector2Int) WorldToLocal((Vector3) position, localTo);
    }

    public static Vector2Int WorldToLocal(this Vector2Int position, Chunk localTo)
    {
        return (Vector2Int) WorldToLocal((Vector3Int) position, localTo);
    }

    public static Vector3Int LocalToWorld(this Vector3 position, Chunk localTo)
    {
        return LocalToWorld(
            new Vector3Int(
                Mathf.FloorToInt(position.x),
                Mathf.FloorToInt(position.y),
                0
            ),
            localTo
        );
    }

    public static Vector3Int LocalToWorld(this Vector3Int position, Chunk localTo)
    {
        return new Vector3Int(
            position.x + localTo.Id.x * localTo.Size.x,
            position.y + localTo.Id.y * localTo.Size.y,
            0
        );
    }

    public static Vector2Int LocalToWorld(this Vector2 position, Chunk localTo)
    {
        return (Vector2Int) LocalToWorld((Vector3) position, localTo);
    }

    public static Vector2Int LocalToWorld(this Vector2Int position, Chunk localTo)
    {
        return (Vector2Int) LocalToWorld((Vector3Int) position, localTo);
    }

    public static Vector2Int ToChunkId(this Vector3 position)
    {
        position.x /= ChunkLoader.Instance.ChunkPrefab.Size.x;
        position.y /= ChunkLoader.Instance.ChunkPrefab.Size.y;
        return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
    }

    public static Vector2Int ToChunkId(this Vector3Int position)
    {
        return ToChunkId((Vector3) position);
    }

    public static Vector2Int ToChunkId(this Vector2 position)
    {
        return ToChunkId((Vector3) position);
    }

    public static Vector2Int ToChunkId(this Vector2Int position)
    {
        return ToChunkId(new Vector3(position.x, position.y));
    }

    public static Tile ToTile(this Vector2Int localPosition, Chunk chunk)
    {
        return chunk.GetTile(localPosition);
    }

    public static Tile ToTile(this Vector2Int worldPosition)
    {
        var chunk = ChunkLoader.Instance.GetChunkById(worldPosition.ToChunkId());
        var localPosition = worldPosition.WorldToLocal(chunk);
        return chunk.GetTile(localPosition);
    }
}
