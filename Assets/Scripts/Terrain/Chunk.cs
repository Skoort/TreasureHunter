using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Vector2Int Id { get; private set; }

    [field: SerializeField]
    public Vector2Int Size { get; private set; }

    private int[] _heightmap;
    private Tile[,] _tiles;

    private ChunkMeshData _rendererMeshData;
    private ChunkMeshData _colliderMeshData;

    private bool _shouldCreateMesh = false;
    //[SerializeField]
    //private MeshFilter _colliderMeshFilter = default;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    public Tile GetTile(Vector2Int position)
    {
        return _tiles[position.x, position.y];
    }

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();
    }

    public void Initialize(Vector2Int id)
    {
        this.Id = id;
        gameObject.name = $"Chunk ({id.x}, {id.y})";
        //Debug.Log($"Chunk ({Id.x}, {Id.y}) initialized!");
    }

    #region --- GENERATION THREAD ---
    private void HandleCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            // Clean up here, then...
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public Task Generate(CancellationToken cancellationToken)
    {
        GenerateHeightmap();
        HandleCancellation(cancellationToken);
        GenerateTiles();
        HandleCancellation(cancellationToken);
        GenerateMeshData();
        HandleCancellation(cancellationToken);

        return Task.CompletedTask;
    }

    private void GenerateHeightmap()
    {
        // Also compute the heights immediately to the left and to the right of this chunk.
        _heightmap = new int[Size.x + 2];
        for (int x = 0; x < _heightmap.Length; ++x)
        {
            int globalX = (x - 1) + Id.x * Size.x;

            var noise1 = Mathf.PerlinNoise(1100000 + globalX * 0.005F, 1000F);
            var noise2 = Mathf.PerlinNoise(1200000 + globalX * 0.005F, 2000F);
            var noise3 = Mathf.PerlinNoise(1300000 + globalX * 0.03F, 3000F);
            var noise4 = Mathf.PerlinNoise(1400000 + globalX * 0.006F, 4000F);
            var noise5 = Mathf.PerlinNoise(1500000 + globalX * 0.01F, 5000F);
            var noise6 = Mathf.PerlinNoise(1600000 + globalX * 0.01F, 6000F);
            var noise7 = Mathf.PerlinNoise(1700000 + globalX * 0.005F, 7000F);

            var flatlandsMinHeight = 0;
            var flatlandsMaxHeight = 15;
            var flatlandsDelta = flatlandsMaxHeight - flatlandsMinHeight;
            var flatlandsMainRatio = 0.8F;
            var flatlandsSecondaryRatio = 1 - flatlandsMainRatio;
            var flatlandsHeight = noise1 * flatlandsDelta * flatlandsMainRatio + noise2 * flatlandsDelta * flatlandsSecondaryRatio + flatlandsMinHeight;
            var mountainsMinHeight = 90;
            var mountainsMaxHeight = 120;
            var mountainsDelta = mountainsMaxHeight - mountainsMinHeight;
            var mountainsMainRatio = 0.9F;
            var mountainsSecondaryRatio = 1 - mountainsMainRatio;
            var mountainsHeight = noise3 * mountainsDelta * mountainsMainRatio + noise4 * mountainsDelta * mountainsSecondaryRatio + mountainsMinHeight;
            var t = noise5 * noise6 * noise7;
            var combinedHeight = Mathf.FloorToInt((1 - t) * flatlandsHeight + t * mountainsHeight);

            _heightmap[x] = combinedHeight;
        }
    }

    private void GenerateTiles()
    {
        _tiles = new Tile[Size.x, Size.y];
        for (int x = 0; x < Size.x; ++x)
        {
            int globalX = x + Id.x * Size.x;
            var groundHeight = _heightmap[x + 1];
            for (int y = 0; y < Size.y; ++y)
            {
                // Get the elevation of the top of the tile.
                var globalHeight = y + Id.y * Size.y + 1;
                if (globalHeight > groundHeight)
                {
                    break;
                }

                var localPosition = new Vector2Int(x, y);
                //var worldPosition = (Vector2) transform.position + localPosition;

                var tile = _tiles[x, y] = new Tile();

                var minDepth = 5;
                var maxDepth = 15;
                var t = Mathf.PerlinNoise(1100000 + globalX * 0.2F, 1000F) * 0.25F + Mathf.PerlinNoise(4000000 + globalX * 0.1F, 1000F) * 0.75F;
                int dirtDepth = Mathf.FloorToInt(Mathf.Lerp(minDepth, maxDepth, t));

                // TODO: Implement a better way to generate tile types.
                tile.Type = globalHeight == groundHeight
                    ? TileType.GRASS
                    : groundHeight - globalHeight < dirtDepth
                        ? TileType.DIRT
                        : TileType.ROCK;
                tile.HasCollision = true;
                tile.Quad = new Quad()
                {
                    Origin = localPosition,
                };
            }
        }
    }

    private void GenerateMeshData()
    {
        _rendererMeshData.Reset();
        _colliderMeshData.Reset();

        int edgeIndex = 0;
        for (int x = 0; x < Size.x; ++x)
        {
            for (int y = 0; y < Size.y; ++y)
            {
                var tile = _tiles[x, y];
                if (tile == null)
                {
                    continue;
                }

                var tl = (Vector3Int)tile.Quad.TopLeftCorner;
                var tr = (Vector3Int)tile.Quad.TopRightCorner;
                var br = (Vector3Int)tile.Quad.BottomRightCorner;
                var bl = (Vector3Int)tile.Quad.BottomLeftCorner;

                #region --- Renderer mesh ---
                SetTileQuad(tile);
                #endregion

                int groundHeightIndex = x + 1;

                #region --- Collider mesh ---
                for (int i = 0; i < 4; ++i)
                {
                    int neighborY = i == 0
                        ? y + 1
                        : i == 2
                            ? y - 1
                            : y;
                    int neighborGlobalHeight = neighborY + Id.y * Size.y + 1;
                    int neighborGroundHeightIndex = i == 1
                        ? groundHeightIndex + 1
                        : i == 3
                            ? groundHeightIndex - 1
                            : groundHeightIndex;
                    int neighborGroundHeight = _heightmap[neighborGroundHeightIndex];
                    if (neighborGlobalHeight <= neighborGroundHeight)
                    {
                        continue;  // The edge is not exposed.
                    }

                    var normal = Vector3.zero;
                    Vector3[] points = new Vector3[4];//null;

                    if (i == 0)
                    {
                        normal = Vector3.up;
                        points = new Vector3[4]
                        {
                            tl - Vector3.forward * 2,
                            tl + Vector3.forward * 2,
                            tr + Vector3.forward * 2,
                            tr - Vector3.forward * 2,
                        };
                    } else
                    if (i == 1)
                    {
                        normal = Vector3.right;
                        points = new Vector3[4]
                        {
                            tr - Vector3.forward * 2,
                            tr + Vector3.forward * 2,
                            br + Vector3.forward * 2,
                            br - Vector3.forward * 2,
                        };
                    } else
                    if (i == 2)
                    {
                        normal = Vector3.down;
                        points = new Vector3[4]
                        {
                            bl + Vector3.forward * 2,
                            bl - Vector3.forward * 2,
                            br - Vector3.forward * 2,
                            br + Vector3.forward * 2,
                        };
                    } else
                    if (i == 3)
                    {
                        normal = Vector3.left;
                        points = new Vector3[4]
                        {
                            tl + Vector3.forward * 2,
                            tl - Vector3.forward * 2,
                            bl - Vector3.forward * 2,
                            bl + Vector3.forward * 2,
                        };
                    }

                    _colliderMeshData.Vertices.AddRange(points);
                    _colliderMeshData.Normals.AddRange(new Vector3[]
                    {
                        normal,
                        normal,
                        normal,
                        normal,
                    });
                    _colliderMeshData.Edges.AddRange(new int[]
                    {
                        edgeIndex,
                        edgeIndex + 1,
                        edgeIndex + 2,
                        edgeIndex + 3,
                    });
                    tile.Quad.EdgeIndices[i] = edgeIndex;

                    edgeIndex += 4;
                }
                #endregion
            }
        }
    }

    public void OnGenerated()
    {
        _shouldCreateMesh = true;
        //Debug.Log($"Chunk ({Id.x}, {Id.y}) finished generating mesh info!");
    }
    #endregion

    private void Update()
    {
        if (_shouldCreateMesh)
        {
            AssignMesh();
            //Debug.Log($"Chunk ({Id.x}, {Id.y}) created mesh!");
            _shouldCreateMesh = false;
        }
    }

    private void AssignMesh()
    {
        _rendererMeshData.Mesh = new Mesh();
        _rendererMeshData.Mesh.SetVertices(_rendererMeshData.Vertices);
        _rendererMeshData.Mesh.SetNormals(_rendererMeshData.Normals);
        _rendererMeshData.Mesh.SetUVs(0, _rendererMeshData.UVs);
        _rendererMeshData.Mesh.SetIndices(_rendererMeshData.Quads, MeshTopology.Quads, 0);
        GetComponent<MeshFilter>().mesh = _rendererMeshData.Mesh;

        _colliderMeshData.Mesh = new Mesh();
        _colliderMeshData.Mesh.SetVertices(_colliderMeshData.Vertices);
        _colliderMeshData.Mesh.SetNormals(_colliderMeshData.Normals);
        _colliderMeshData.Mesh.SetIndices(_colliderMeshData.Edges, MeshTopology.Quads, 0);
        //_colliderMeshFilter.mesh = _colliderMeshData.Mesh;
        _meshCollider.sharedMesh = _colliderMeshData.Mesh;
    }

    public void SetVisibility(bool isVisible)
    {
        _meshRenderer.enabled = isVisible;
    }

    public void SetCollideability(bool isCollideable)
    {
        _meshCollider.enabled = isCollideable;
    }

    private int GetNextQuadIndex()
    {
        if (_rendererMeshData.FreeIndices.Count > 0)
        {
            return _rendererMeshData.FreeIndices.Dequeue();
        }
        else
        {
            return _rendererMeshData.Quads.Count;
        }
    }

    private int GetNextEdgeIndex()
    {
        if (_colliderMeshData.FreeIndices.Count > 0)
        {
            return _colliderMeshData.FreeIndices.Dequeue();
        }
        else
        {
            return _colliderMeshData.Edges.Count;
        }
    }
    
    private void UnsetTileQuad(Tile tile)
    {
        var quad = tile.Quad;
        if (quad.QuadIndex != -1)
        {
            _rendererMeshData.FreeIndices.Enqueue(quad.QuadIndex);
            for (int i = 0; i < 4; ++i)
            {
                _rendererMeshData.Quads[quad.QuadIndex + i] = 0;
            }
            quad.QuadIndex = -1;
        }
    }

    private void SetTileQuad(Tile tile)
    {
        var quad = tile.Quad;
        if (quad.QuadIndex == -1)
        {
            quad.QuadIndex = GetNextQuadIndex();
        }
        var tl = (Vector3Int)tile.Quad.TopLeftCorner;
        var tr = (Vector3Int)tile.Quad.TopRightCorner;
        var br = (Vector3Int)tile.Quad.BottomRightCorner;
        var bl = (Vector3Int)tile.Quad.BottomLeftCorner;
        var vertices = new Vector3[] {
            tl,
            tr,
            br,
            bl,
        };
        var normals = new Vector3[]
        {
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, -1),
        };
        var uvs = new Vector3[]
        {
            new Vector3(0, 1, (int)tile.Type),
            new Vector3(1, 1, (int)tile.Type),
            new Vector3(1, 0, (int)tile.Type),
            new Vector3(0, 0, (int)tile.Type),
        };
        var quads = new int[]
        {
            quad.QuadIndex,
            quad.QuadIndex + 1,
            quad.QuadIndex + 2,
            quad.QuadIndex + 3,
        };
        if (quad.QuadIndex == _rendererMeshData.Quads.Count)
        {
            _rendererMeshData.Vertices.AddRange(vertices);
            _rendererMeshData.Normals.AddRange(normals);
            _rendererMeshData.UVs.AddRange(uvs);
            _rendererMeshData.Quads.AddRange(quads);
        }
        else
        {
            for (int i = 0; i < 4; ++i)
            {
                _rendererMeshData.Vertices[quad.QuadIndex + i] = vertices[i];
                _rendererMeshData.Normals[quad.QuadIndex + i] = normals[i];
                _rendererMeshData.UVs[quad.QuadIndex + i] = uvs[i];
                _rendererMeshData.Quads[quad.QuadIndex + i] = quads[i];
            }
        }
    }

    private void UnsetTileCollision(Tile tile)
    {
        var quad = tile.Quad;
        for (int i = 0; i < 4; ++i)
        {
            var edgeIndex = quad.EdgeIndices[i];
            if (edgeIndex != -1)
            {
                _colliderMeshData.FreeIndices.Enqueue(edgeIndex);
                for (int j = 0; j < 4; ++j)
                {
                    _colliderMeshData.Edges[edgeIndex + j] = 0;
                }
                quad.EdgeIndices[i] = -1;
            }
        }
    }

    private void UpdateTileCollision(Tile tile)
    {
        if (!tile.HasCollision)
        {
            // This tile shouldn't have collisions. Remove any previous ones.
            UnsetTileCollision(tile);
            return;
        }

        var quad = tile.Quad;
        int i = 0;
        foreach (var neighborLocalPosition in quad.GetNeighboringPositions())
        {
            var edgeIndex = quad.EdgeIndices[i];

            var neighborWorldPosition = neighborLocalPosition.LocalToWorld(this);
            var neighborChunkId = neighborWorldPosition.ToChunkId();
            var neighborChunk = ChunkLoader.Instance.GetChunkById(neighborChunkId);
            var neighborLocalPosition2 = neighborWorldPosition.WorldToLocal(neighborChunk);
            var neighborTile = neighborChunk.GetTile(neighborLocalPosition2);
            if (neighborTile == null || !neighborTile.HasCollision)
            {
                // There should be a collider at this edge. Add it if it doesn't exist.
                if (edgeIndex == -1)
                {
                    edgeIndex = GetNextEdgeIndex();

                    var tl = (Vector3Int)tile.Quad.TopLeftCorner;
                    var tr = (Vector3Int)tile.Quad.TopRightCorner;
                    var br = (Vector3Int)tile.Quad.BottomRightCorner;
                    var bl = (Vector3Int)tile.Quad.BottomLeftCorner;

                    var normal = Vector3.zero;
                    Vector3[] points = null;

                    if (i == 0)
                    {
                        normal = Vector3.up;
                        points = new Vector3[4]
                        {
                            tl - Vector3.forward * 2,
                            tl + Vector3.forward * 2,
                            tr + Vector3.forward * 2,
                            tr - Vector3.forward * 2,
                        };
                    } else
                    if (i == 1)
                    {
                        normal = Vector3.right;
                        points = new Vector3[4]
                        {
                            tr - Vector3.forward * 2,
                            tr + Vector3.forward * 2,
                            br + Vector3.forward * 2,
                            br - Vector3.forward * 2,
                        };
                    } else
                    if (i == 2)
                    {
                        normal = Vector3.down;
                        points = new Vector3[4]
                        {
                            bl + Vector3.forward * 2,
                            bl - Vector3.forward * 2,
                            br - Vector3.forward * 2,
                            br + Vector3.forward * 2,
                        };
                    } else
                    if (i == 3)
                    {
                        normal = Vector3.left;
                        points = new Vector3[4]
                        {
                            tl + Vector3.forward * 2,
                            tl - Vector3.forward * 2,
                            bl - Vector3.forward * 2,
                            bl + Vector3.forward * 2,
                        };
                    }

                    var normals = new Vector3[4]
                    {
                        normal,
                        normal,
                        normal,
                        normal,
                    };
                    var edges = new int[4]
                    {
                        edgeIndex,
                        edgeIndex + 1,
                        edgeIndex + 2,
                        edgeIndex + 3,
                    };

                    if (edgeIndex == _colliderMeshData.Edges.Count)
                    {
                        _colliderMeshData.Vertices.AddRange(points);
                        _colliderMeshData.Normals.AddRange(normals);
                        _colliderMeshData.Edges.AddRange(edges);
                    }
                    else
                    {
                        for (int j = 0; j < 4; ++j)
                        {
                            _colliderMeshData.Vertices[edgeIndex + j] = points[j];
                            _colliderMeshData.Normals[edgeIndex + j] = normals[j];
                            _colliderMeshData.Edges[edgeIndex + j] = edges[j];
                        }
                    }

                    quad.EdgeIndices[i] = edgeIndex;
                }
            }
            else
            {
                // There should be no collider at this edge. Remove it if it exists.
                if (edgeIndex != -1)
                {
                    _colliderMeshData.FreeIndices.Enqueue(edgeIndex);
                    for (int j = 0; j < 4; ++j)
                    {
                        _colliderMeshData.Edges[edgeIndex + j] = 0;
                    }
                    quad.EdgeIndices[i] = -1;
                }
            }

            ++i;
        }
    }

    private IEnumerable<Chunk> UpdateTileNeighbors(Tile tile)
    {
        var modifiedChunks = new HashSet<Chunk>() { this };
        foreach (var neighborPosition in tile.Quad.GetNeighboringPositions())
        {
            var neighborWorldPosition = neighborPosition.LocalToWorld(this);
            var neighborChunkId = neighborWorldPosition.ToChunkId();
            var neighborChunk = ChunkLoader.Instance.GetChunkById(neighborChunkId);
            var neighborLocalPosition2 = neighborWorldPosition.WorldToLocal(neighborChunk);
            var neighborTile = neighborChunk.GetTile(neighborLocalPosition2);
            if (neighborTile != null)
            {
                neighborChunk.UpdateTileCollision(neighborTile);
                modifiedChunks.Add(neighborChunk);
            }
        }
        return modifiedChunks;
    }

    public void UnsetTile(Vector2Int position)
    {
        var tile = _tiles[position.x, position.y];
        if (tile != null)
        {
            UnsetTileQuad(tile);

            tile.HasCollision = false;
            UpdateTileCollision(tile);
            var modifiedChunks = UpdateTileNeighbors(tile);
            _tiles[position.x, position.y] = null;

            foreach (var chunk in modifiedChunks)
            {
                chunk.AssignMesh();
            }
        }
    }

    public void SetTile(Vector2Int position, TileType newTileType)
    {
        var tile = _tiles[position.x, position.y];
        if (tile == null)
        {
            tile = new Tile()
            {
                Quad = new Quad()
                {
                    Origin = position,
                    QuadIndex = -1,
                },
                HasCollision = true,
            };
            _tiles[position.x, position.y] = tile;
        }
        tile.Type = newTileType;
        SetTileQuad(tile);

        UpdateTileCollision(tile);
        var modifiedChunks = UpdateTileNeighbors(tile);

        foreach (var chunk in modifiedChunks)
        {
            chunk.AssignMesh();
        }
    }
}
