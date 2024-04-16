using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum ChunkState
{
    AWAITING_INSTANTIATION,
    AWAITING_GENERATION,
    GENERATED,
    AWAITING_DESTRUCTION,
    DESTROYED,
}

public class ChunkLoadInfo : IDisposable
{
    public Vector2Int Id { get; }
    public Chunk Chunk { get; set; }
    public ChunkState State { get; set; }
    public Task GenerationTask { get; set; }
    public CancellationTokenSource GenerationTaskCts { get; }
    public float TimeOfLastTick { get; set; }

    public ChunkLoadInfo(Vector2Int id)
    {
        Id = id;
        Chunk = null;
        State = ChunkState.AWAITING_INSTANTIATION;
        GenerationTask = null;
        GenerationTaskCts = new CancellationTokenSource();
        TimeOfLastTick = Time.time;
    }

    public void CancelGeneration()
    {
        GenerationTaskCts.Cancel();
    }

    public void Dispose()
    {
        GenerationTaskCts.Dispose();
    }
}

public class ChunkLoader : MonoBehaviour
{
    public static ChunkLoader Instance;

    [field: SerializeField]
    public Chunk ChunkPrefab { get; private set; }

    private Coroutine _tickCoroutine;
    private Coroutine _instantiateCoroutine;
    private Coroutine _generateCoroutine;
    private Coroutine _destroyCoroutine;

    [SerializeField]
    private float _chunkRefreshesPerSecond = 2;
    [SerializeField]
    private float _instantiatesPerSecond = 20;
    [SerializeField]
    private float _generatesPerSecond = 15;
    [SerializeField]
    private float _destroysPerSecond = 10;

    [SerializeField]
    private int _maxActiveGenerateTasks = 10;
    private int _numActiveGenerateTasks = 0;

    private LinkedList<ChunkLoadInfo> _chunksToInstantiate;
    private LinkedList<ChunkLoadInfo> _chunksToGenerate;
    private List<ChunkLoadInfo> _chunksToDestroy;
    private Dictionary<Vector2Int, ChunkLoadInfo> _chunks;
    private Queue<Exception> _loadErrors;

    [SerializeField]
    private Transform _followTarget = default;

    [SerializeField]
    private float _activeDistance = 20;
    [SerializeField]
    private float _inactiveDistance = 50;

    public Chunk GetChunkById(Vector2Int id)
    {
        _chunks.TryGetValue(id, out var chunkInfo);
        return chunkInfo?.Chunk;
    }

    private void Awake()
    {
        Debug.Assert(Instance == null, "Instance of ChunkLoader already exists!");

        Instance = this;
        _chunks = new Dictionary<Vector2Int, ChunkLoadInfo>();
        _chunksToInstantiate = new LinkedList<ChunkLoadInfo>();
        _chunksToGenerate = new LinkedList<ChunkLoadInfo>();
        _chunksToDestroy = new List<ChunkLoadInfo>();
        _loadErrors = new Queue<Exception>();
        _tickCoroutine = StartCoroutine(IE_OnTick());
        _instantiateCoroutine = StartCoroutine(IE_OnInstantiateTick());
        _generateCoroutine = StartCoroutine(IE_OnGenerateTick());
        _destroyCoroutine = StartCoroutine(IE_OnDestroyTick());
    }

    private void OnDestroy()
    {
        foreach (var chunkInfo in _chunks.Values)
        {
            chunkInfo.CancelGeneration();
            chunkInfo.Dispose();
        }

        StopCoroutine(_tickCoroutine);
        StopCoroutine(_instantiateCoroutine);
        StopCoroutine(_generateCoroutine);
        StopCoroutine(_destroyCoroutine);
        Instance = null;
    }

    private IEnumerator IE_OnTick()
    {
        float waitTime = 1 / _chunkRefreshesPerSecond;
        while (true)
        {
            var nX = _inactiveDistance / ChunkPrefab.Size.x;
            var nY = _inactiveDistance / ChunkPrefab.Size.y;
            for (float offsetY = -nY; offsetY <= +nY; ++offsetY)
            for (float offsetX = -nX; offsetX <= +nX; ++offsetX)
            {
                var offsetTargetPosition = new Vector2(
                    _followTarget.position.x + ChunkPrefab.Size.x * offsetX,
                    _followTarget.position.y + ChunkPrefab.Size.y * offsetY);
                var chunkId = offsetTargetPosition.ToChunkId();

                // Update the chunk.
                if (!_chunks.TryGetValue(chunkId, out var chunkInfo))
                {
                    //Debug.Log($"Chunk ({chunkId.x}, {chunkId.y}) was created.");
                    chunkInfo = new ChunkLoadInfo(chunkId);
                    _chunks[chunkId] = chunkInfo;
                    _chunksToInstantiate.AddLast(chunkInfo);
                }
                chunkInfo.TimeOfLastTick = Time.time;
            }

            // Queue for deletion those chunks that haven't been updated in too long.
            var newlyQueuedForDestruction = new List<ChunkLoadInfo>();
            foreach (var chunkInfo in _chunks.Values)
            {
                if (chunkInfo.State == ChunkState.GENERATED)
                {
                    // Enable/Disable collision and rendering based on distance.
                    var chunkCenter = chunkInfo.Id * ChunkPrefab.Size + (Vector2)ChunkPrefab.Size * 0.5F;
                    var isInRange = Vector2.Distance(_followTarget.position, chunkCenter) <= _activeDistance;
                    chunkInfo.Chunk.SetVisibility(isInRange);
                    chunkInfo.Chunk.SetCollideability(isInRange);
                }

                var timeSinceLastTick = Time.time - chunkInfo.TimeOfLastTick;
                if (timeSinceLastTick > 10)
                {
                    //Debug.Log($"Chunk ({chunkInfo.Id.x}, {chunkInfo.Id.y}) was queued for destruction.");

                    var node1 = _chunksToGenerate.Find(chunkInfo);
                    if (node1 != null)
                    {
                        _chunksToGenerate.Remove(node1);
                    }
                    var node2 = _chunksToInstantiate.Find(chunkInfo);
                    if (node2 != null)
                    {
                        _chunksToInstantiate.Remove(node2);
                    }

                    chunkInfo.State = ChunkState.AWAITING_DESTRUCTION;
                    chunkInfo.Chunk?.gameObject?.SetActive(false);
                    chunkInfo.CancelGeneration();
                    _chunksToDestroy.Add(chunkInfo);
                    newlyQueuedForDestruction.Add(chunkInfo);
                }
            }

            foreach (var chunkInfo in newlyQueuedForDestruction)
            {
                _chunks.Remove(chunkInfo.Id);
            }

            yield return new WaitForSeconds(waitTime);
        }
    }

    private ChunkLoadInfo TakeNearestChunk(LinkedList<ChunkLoadInfo> chunks)
    {
        var minDistance = Mathf.Infinity;
        var nearestNode = (LinkedListNode<ChunkLoadInfo>)null;
        for (var node = chunks.First; node != null; node = node.Next)
        {
            var chunkCenter = node.Value.Id * ChunkPrefab.Size + (Vector2)ChunkPrefab.Size * 0.5F;
            var dist = Vector2.Distance(_followTarget.position, chunkCenter);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestNode = node;
            }
        }
        chunks.Remove(nearestNode);
        return nearestNode.Value;
    }

    private IEnumerator IE_OnInstantiateTick()
    {
        float waitTime = 1 / _instantiatesPerSecond;
        while (true)
        {
            if (_chunksToInstantiate.Count > 0)
            {
                var chunkInfo = TakeNearestChunk(_chunksToInstantiate);

                Debug.Assert(chunkInfo.State == ChunkState.AWAITING_INSTANTIATION,
                    $"Attempted to instantiate already instantiated chunk with ID (x: {chunkInfo.Id.x} y: {chunkInfo.Id.y})!");

                var id = chunkInfo.Id;
                var position = new Vector3(
                    ChunkPrefab.Size.x * id.x,
                    ChunkPrefab.Size.y * id.y);
                var chunk = Instantiate(
                    ChunkPrefab,
                    position,
                    Quaternion.identity,
                    transform);
                chunk.Initialize(id);
                chunkInfo.Chunk = chunk;
                chunkInfo.State = ChunkState.AWAITING_GENERATION;
                _chunksToGenerate.AddLast(chunkInfo);
            }
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator IE_OnGenerateTick()
    {
        float waitTime = 1 / _generatesPerSecond;
        while (true)
        {
            if (_chunksToGenerate.Count > 0 && _numActiveGenerateTasks < _maxActiveGenerateTasks)
            {
                var chunkInfo = TakeNearestChunk(_chunksToGenerate);

                // Run the generation task.
                var cancellationToken = chunkInfo.GenerationTaskCts.Token;
                Interlocked.Increment(ref _numActiveGenerateTasks);
                chunkInfo.GenerationTask = Task.Run(async () =>
                {
                    try
                    {
                        await chunkInfo.Chunk.Generate(cancellationToken);
                        chunkInfo.Chunk.OnGenerated();
                        chunkInfo.State = ChunkState.GENERATED;
                    }
                    catch (Exception e)
                    {
                        _loadErrors.Enqueue(e);
                    }
                }, cancellationToken);
                chunkInfo.GenerationTask.ContinueWith((task) =>
                {
                    Interlocked.Decrement(ref _numActiveGenerateTasks);
                });
            }
            yield return new WaitForSeconds(waitTime);
        }
    }

    private IEnumerator IE_OnDestroyTick()
    {
        float waitTime = 1 / _destroysPerSecond;
        while (true)
        {
            var indexToRemove = _chunksToDestroy.FindIndex(
                (chunkInfo) => chunkInfo.GenerationTask?.IsCompleted ?? true);
            if (indexToRemove != -1)
            {
                var chunkInfo = _chunksToDestroy[indexToRemove];

                _chunksToDestroy[indexToRemove] = _chunksToDestroy.Last();
                _chunksToDestroy.RemoveAt(_chunksToDestroy.Count - 1);

                if (chunkInfo.Chunk)
                {
                    Destroy(chunkInfo.Chunk.gameObject);
                }
                chunkInfo.Dispose();
            }
            yield return new WaitForSeconds(waitTime);
        }
    }
}
