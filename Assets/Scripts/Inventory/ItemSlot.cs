using UnityEngine;

[System.Serializable]
public class ItemSlot
{
    [field: SerializeField]
    public ItemData Item { get; set; }
    [field: SerializeField]
    public int Quantity { get; set; }
}
