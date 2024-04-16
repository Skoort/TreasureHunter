using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField, Tooltip("A negative value means that there is no limit.")]
    private int _maxSlots = -1;
    [SerializeField, ReadOnly]
    private List<ItemSlot> _slots;

    private void Awake()
    {
        _slots = new List<ItemSlot>();
    }

    // Try to add the desired amount of item to the inventory and return how many were added.
    public int Add(ItemData item, int quantity = 1)
    {
        int amountAdded = 0;

        for (int i = 0; i < _slots.Count; ++i)
        {
            if (quantity < 1)
            {
                return amountAdded;
            }

            var slot = _slots[i];
            var room = item.MaxStackSize - slot.Quantity;
            if (slot.Item == item && room > 0)
            {
                var added = Math.Min(quantity, room);
                slot.Quantity += added;
                quantity -= added;
                amountAdded += added;
            }
        }

        while (quantity > 0 && (_maxSlots < 0 || _slots.Count < _maxSlots))
        {
            var added = Math.Min(quantity, item.MaxStackSize);
            _slots.Add(new ItemSlot() { Item = item, Quantity = added });
            quantity -= added;
            amountAdded += added;
        }

        return amountAdded;
    }

    // Try to remove the desired amount of item from the inventory and return how many were removed.
    public int Remove(ItemData item, int quantity = 1)
    {
        int amountRemoved = 0;

        var slotsToRemove = new List<ItemSlot>();

        for (int i = 0; i < _slots.Count; ++i)
        {
            if (quantity < 1)
            {
                break;
            }

            var slot = _slots[i];
            var room = slot.Quantity;
            if (slot.Item == item && room > 0)
            {
                var removed = Math.Min(quantity, room);
                slot.Quantity -= removed;
                quantity -= removed;
                amountRemoved += removed;
            }

            if (slot.Quantity <= 0)
            {
                slotsToRemove.Add(slot);
            }
        }

        _slots = _slots.Where(slot => !slotsToRemove.Contains(slot)).ToList();

        return amountRemoved;
    }

    public bool IsEmpty()
    {
        return _slots.Count == 0;
    }

    public ItemData FirstItem()
    {
        return _slots.First().Item;
    }
}
