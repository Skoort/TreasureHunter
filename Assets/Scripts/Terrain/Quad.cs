using System.Collections.Generic;
using UnityEngine;

public class Quad
{
    public Vector2Int Origin { get; set; }
    public Vector2Int Size { get; set; } = Vector2Int.one;
    public Vector2Int TopLeftCorner => new Vector2Int(BottomLeftCorner.x, BottomLeftCorner.y + Size.y);
    public Vector2Int TopRightCorner => new Vector2Int(TopLeftCorner.x + Size.x, TopLeftCorner.y);
    public Vector2Int BottomRightCorner => new Vector2Int(TopRightCorner.x, TopRightCorner.y - Size.y);
    public Vector2Int BottomLeftCorner => Origin;
    public int QuadIndex { get; set; } = -1;
    public int[] EdgeIndices { get; } = new int[] { -1, -1, -1, -1 };

    public IEnumerable<Vector2Int> GetNeighboringPositions()
    {
        return new Vector2Int[]
        {
            Origin + new Vector2Int( 0,  1),
            Origin + new Vector2Int( 1,  0),
            Origin + new Vector2Int( 0, -1),
            Origin + new Vector2Int(-1,  0),
        };
    }
}
