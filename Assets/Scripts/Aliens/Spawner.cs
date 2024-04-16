using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    // TODO: Make a singleton.

    [SerializeField] private Transform _player = default;
    [SerializeField] private Transform[] _alienPrefabs = new Transform[] { };

    [SerializeField] private float _minSpawnDistance = 10;
    [SerializeField] private float _maxSpawnDistance = 20;
 
    [SerializeField] private float _spawnInterval = 2;
    private float _spawnTimer;

    private int _chunkLayerMask;

    private void Awake()
    {
        _chunkLayerMask = 1 << LayerMask.NameToLayer("Chunk");
        _spawnTimer = _spawnInterval;
    }

    private void Update()
    {
        // TODO: A _spawnInterval of 0.1F crashed my game on start up.
        _spawnTimer -= Time.deltaTime;

        if (_spawnTimer <= 0)
        {
            _spawnTimer = _spawnInterval;

            Spawn();
        }
    }

    private void Spawn()
    {
        var prefabId = Random.Range(0, _alienPrefabs.Length);
        var prefab = _alienPrefabs[prefabId];

        // Find all suitable upwards facing edges. Use repeated Raycasts because RaycastAll
        // doesn't register repeated hits with the same collider.
        var hits = new List<RaycastHit>();
        var xOffset = Random.Range(_minSpawnDistance, _maxSpawnDistance) * Mathf.Sign(Random.value - 0.5F);
        var yOffset = 50.0F;
        while (yOffset > -50)
        {
            var start = _player.position + new Vector3(xOffset, yOffset);
            var range = 50 + yOffset;
            if (Physics.Raycast(start, Vector3.down, out var hitInfo, range, _chunkLayerMask, QueryTriggerInteraction.Ignore))
            {
                // Subtract a tiny value from yOffset so that we don't hit the same edge again.
                yOffset = hitInfo.point.y - _player.position.y - 0.05F;
                hits.Add(hitInfo);
            }
            else
            {
                break;  // No more hits. Break.
            }
        }

        if (hits.Count == 0)
        {
            return;  // Found no suitable upwards facing edges.
        }

        var chosenHit = hits[Random.Range(0, hits.Count)];
        SpawnAlienAt(prefab, chosenHit.point);
    }

    private void SpawnAlienAt(Transform alienPrefab, Vector3 position)
    {
        // TODO: Check if the alien actually fits in the area.
        Instantiate(alienPrefab, position, Quaternion.identity);
    }

    // TODO: Add a function that can be used to spawn entities at will.
}
