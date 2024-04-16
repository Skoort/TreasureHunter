using UnityEngine;

public enum TileType
{
    GRASS = 0,
    DIRT,
    ROCK,
    COPPER,
    IRON,
    GOLD,
    RUBY,
    SAPPHIRE,
    EMERALD,
    DIAMOND,
}

public class Tile
{
    public Vector2Int Position { get; private set; }

    public TileType Type { get; set; }

    // Reference to the Quad that occupies this Tile.
    public Quad Quad { get; set; }

    public bool HasCollision { get; set; }
}
