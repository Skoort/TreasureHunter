using UnityEngine;

public enum WeaponSlot  // For now, this only contains ranged weapon types.
{
    SIDE_ARM,
    PRIMARY,
    HEAVY
}

// Consider refactoring this into Weapon, MeleeWeapon, RangedWeapon, ProjectileWeapon, EmitterWeapon & HitscanWeapon. Differentiation between single-shot and automatic weapons is handled by the FireController class (What about burst weapons?). Need to look up how to serialize polymorphic data. Look into SerializedReference.
public abstract class Weapon : ScriptableObject
{
    public string Name;

    public WeaponSlot Slot;

    public float MinDamage;
    public float MaxDamage;

    public Transform _itemPrefab;
    public Transform _equippedPrefab;
}
