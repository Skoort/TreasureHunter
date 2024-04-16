using UnityEngine;

public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError($"Attempted to make another DropManager instance {this.name}!");
            Destroy(this.transform.root.gameObject);
        }
    }

    [SerializeField]
    private Drop _itemDropPrefab = default;

    public void SpawnDrop(ItemData item, Vector3 position)
    {
        var gameObject = ObjectPool.Instance.RequestObject(
            _itemDropPrefab.gameObject,
            position,
            Quaternion.identity,
            this.transform
        );
        var drop = gameObject.GetComponent<Drop>();
        drop.AssignItem(item);
    }

    public void DespawnDrop(Drop drop)
    {
        ObjectPool.Instance.ReleaseObject(drop.gameObject);
    }
}
