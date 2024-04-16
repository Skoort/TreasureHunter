using System.Collections.Generic;
using UnityEngine;

// TODO: Write a custom editor script that makes editing this easy.
public class TileToItemMapping : MonoBehaviour
{
    public static TileToItemMapping Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError($"Attempted to make another TileToItemMapping instance {this.name}!");
            Destroy(this.transform.root.gameObject);
        }
    }

    [SerializeField]
    private List<ItemData> _orderedItems = default;

    public ItemData GetItem(TileType type)
    {
        var item = _orderedItems[(int)type];
        Debug.Assert(item != null, $"Tile {type} does not have a corresponding item!");
        return item;
    }

    public TileType GetTile(ItemData item)
    {
        var index = _orderedItems.FindIndex((item2) => item2 == item);
        Debug.Assert(index != -1, $"Item {item.Name} does not have a corresponding tile!");
        return (TileType)index;
    }
}
