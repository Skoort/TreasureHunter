using System.Collections;
using UnityEngine;

public class PlaceAndPickupTiles : MonoBehaviour
{
	[SerializeField] private Transform _shootOrigin = null;

	private Inventory _playerInventory;

    private void Awake()
    {
		_playerInventory = GetComponent<Inventory>();
    }

    private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			InteractWithBlock(placeOrPickup: false);
		}
		else
		if (Input.GetKeyDown(KeyCode.Mouse1))
		{
			InteractWithBlock(placeOrPickup: true);
		}
	}

	private void InteractWithBlock(bool placeOrPickup)
	{
		var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		targetPosition.z = 0;

		var direction = targetPosition - _shootOrigin.position;

		var layerMask = 1 << LayerMask.NameToLayer("Chunk");  // We only want to intersect the Chunk.

		Debug.DrawLine(_shootOrigin.position, _shootOrigin.position + direction.normalized * 3f, Color.blue, 1f);

		if (Physics.Raycast(_shootOrigin.position, direction, out var hitInfo, 3, layerMask))
		{
			Debug.Log("HIT CHUNK!");

			var worldPosToAffect = hitInfo.point + (placeOrPickup ? +1 : -1) * hitInfo.normal.normalized * 0.5F;
			var chunkToAffect = ChunkLoader.Instance.GetChunkById(worldPosToAffect.ToChunkId());
			var localPosToAffect = worldPosToAffect.WorldToLocal(chunkToAffect);

			Debug.Log($"Hit block: {chunkToAffect.transform.position + localPosToAffect}");
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}
			_coroutine = StartCoroutine(DrawCube(chunkToAffect.transform.position + localPosToAffect + Vector3.one * 0.5F));

			if (placeOrPickup)
			{
				if (!_playerInventory || _playerInventory.IsEmpty())
                {
					return;
                }

				var item = _playerInventory.FirstItem();
				_playerInventory.Remove(item);
				var tileType = TileToItemMapping.Instance.GetTile(item);
				chunkToAffect.SetTile((Vector2Int)localPosToAffect, tileType);
				//PushOutAllItemDropsInBlock(chunkToAffect, posToAffect);  // How to push items out of big areas like furniture? My suggestion is to take that furnitures bounds as the rectangular prism.
			}
			else
			{
				var tileType = chunkToAffect.GetTile((Vector2Int)localPosToAffect).Type;
				chunkToAffect.UnsetTile((Vector2Int)localPosToAffect);

				// TODO: Here is where we would do a check to make sure that if grass is destroyed with a non silktouch tool, then it turns to grass.
				if (tileType == TileType.GRASS)
                {
					tileType = TileType.DIRT;
                }

				var item = TileToItemMapping.Instance.GetItem(tileType);
				var position = Vector3Int.FloorToInt(worldPosToAffect) + new Vector3(0.5F, 0.5F, 0.5F);
				DropManager.Instance.SpawnDrop(item, position);
			}
		}
	}

	//private void PushOutAllItemDropsInBlock(Chunk chunk, Vector3Int localPositionToPlace)
	//{
	//	var neighboringPositions = chunk.GetNeighboringPositions(localPositionToPlace);

	//	var chunkPos = chunk.gameObject.transform.position;
	//	var globalBlockOrigin = chunkPos + localPositionToPlace + new Vector3(0.5F, 0.5F, 0.5F);

	//	var colliders = Physics.OverlapBox(globalBlockOrigin, Vector3.one * 0.5F, Quaternion.identity, LayerMask.GetMask("Item Drop"));
	//	foreach (var collider in colliders)
	//	{
	//		var itemPos = collider.attachedRigidbody.position;

	//		var smallestDir = Vector3.zero;
	//		var smallestDeltaMag = float.MaxValue;
	//		foreach (var neighboringPos in neighboringPositions)
	//		{
	//			Vector3 neighborDir = neighboringPos - localPositionToPlace;
	//			var neighbor = chunk.GetVoxel(neighboringPos);

	//			if (neighbor != null)
	//			{ 
	//				continue;  // The neighboring block is occupied. We want to find an unoccupied one to push this item to. Skip.
	//			}

	//			// Calculates the distance of the item to the neighboring edge.
	//			// Here we rely on the alternate definition of the dot product
	//			// a dot b = a.x * b.x + a.y * b.y + a.z * b.z
	//			// to eliminate any the two axes not in the direction of the neighbor.
	//			var delta = (globalBlockOrigin + neighborDir * 0.5F) - itemPos;
	//			var deltaMag = Vector3.Dot(delta, neighborDir);
	//			if (smallestDeltaMag > deltaMag)
	//			{
	//				smallestDir = neighborDir;
	//				smallestDeltaMag = deltaMag;
	//			}
	//		}

	//		// Add a small constant to the smallestDeltaMag to represent the size of the ItemDrop.
	//		collider.transform.position += smallestDir * (smallestDeltaMag + 0.2F);
	//	}
	//}

	private IEnumerator DrawCube(Vector3 pos)
	{
		_cubeCenter = pos;
		yield return new WaitForSeconds(0.1F);
		_cubeCenter = null;
	}

	private Vector3? _cubeCenter;
	private Coroutine _coroutine;
	private void OnDrawGizmos()
	{
		if (_cubeCenter != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawCube(_cubeCenter.Value, Vector3.one);
		}
	}
}