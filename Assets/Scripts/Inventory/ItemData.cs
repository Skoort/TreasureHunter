using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [field: SerializeField]
    public int Id { get; set; }

    [field: SerializeField]
    public string Name { get; set; }
    [field: SerializeField]
    public string Description { get; set; }

    [field: SerializeField]
    public int MaxStackSize { get; set; }

    [field: SerializeField]
    public Sprite Sprite { get; set; }
}
