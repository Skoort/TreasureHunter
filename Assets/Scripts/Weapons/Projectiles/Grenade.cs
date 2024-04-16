using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float _explosionRadius = 10;
    [SerializeField] private float _fuseDuration = 5;

    private float _timer = 0;
    private bool _hasExploded = false;

    private float _buffer = 0.05F;

    private void Update()
    {
        if (!_hasExploded)
        {
            _timer += Time.deltaTime;
            if (_timer >= _fuseDuration)
            {
                StartCoroutine(IE_Explode());
            }
        }
    }

    private IEnumerator IE_Explode()
    {
        _hasExploded = true;

        var origin = Vector3Int.FloorToInt(transform.position);
        var done = new List<(Vector3Int, float)>();
        var todo = new Queue<Vector3Int>(new Vector3Int[] { origin });
        while (todo.Count > 0)
        {
            var position = todo.Dequeue();
            if (done.FindIndex((other) => position == other.Item1) != -1)
            {
                // Already processed this position.
                continue;
            }

            var distance = Vector3.Distance(origin, position);
            if (distance > _explosionRadius * 1.415 + _buffer)
            {
                // We have checked every position in the bounding box of the explosion radius.
                break;
            }
            if (distance > _explosionRadius + _buffer)
            {
                // This position is outside of the explosion radius.
                continue;
            }

            todo.Enqueue(position + Vector3Int.up);
            todo.Enqueue(position + Vector3Int.right);
            todo.Enqueue(position + Vector3Int.down);
            todo.Enqueue(position + Vector3Int.left);
            done.Add((position, distance));
        }

        done.Sort((pos1, pos2) => pos1.Item2.CompareTo(pos2.Item2));

        int currDistance = 0;
        foreach (var position in done)
        {
            if (position.Item2 > currDistance + _buffer)
            {
                // Every layer of the explosion has a small delay.
                ++currDistance;
                yield return new WaitForSeconds(0.015F);
            }

            var chunkToAffect = ChunkLoader.Instance.GetChunkById(position.Item1.ToChunkId());
            var localPosition = position.Item1.WorldToLocal(chunkToAffect);

            chunkToAffect.UnsetTile((Vector2Int)localPosition);
        }

        Destroy(this.gameObject);
    }
}
