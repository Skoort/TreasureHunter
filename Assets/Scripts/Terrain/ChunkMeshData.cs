using System.Collections.Generic;
using UnityEngine;

public struct ChunkMeshData
{
    public Mesh Mesh { get; set; }
    public List<Vector3> Vertices { get; private set; }
    public List<Vector3> Normals { get; private set; }
    public List<Vector3> UVs { get; private set; }  // The z element represents the tile type (the index for the texture array).
    public List<int> Quads { get; private set; }
    public List<int> Edges { get; private set; }
    public Queue<int> FreeIndices { get; private set; }

    public void Reset()
    {
        Vertices = new List<Vector3>();
        Normals = new List<Vector3>();
        UVs = new List<Vector3>();
        Quads = new List<int>();
        Edges = new List<int>();
        FreeIndices = new Queue<int>();
    }
}
