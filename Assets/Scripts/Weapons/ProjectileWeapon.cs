using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Ranged/Projectile")]
public class ProjectileWeapon : RangedWeapon
{
    public GameObject ProjectilePrefab => _projectilePrefab;

    [SerializeField] private GameObject _projectilePrefab;
}
