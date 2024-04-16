using UnityEngine;

public class ProjectileFiringSystem : FiringSystem
{
    [SerializeField, Layer]
	protected LayerMask _projectileLayer = default;

	[SerializeField]
	private float _projectileSpeed = 10;

	protected override void DoFire()
	{
		var weapon = (ProjectileWeapon)Weapon;

		var direction = Target == null
			? Origin.forward
			: (Target.position - Origin.position).normalized;
		var aimPoint = direction * 100 + Origin.position + Random.insideUnitSphere * Weapon.Spread;

		var gameObject = ObjectPool.Instance.RequestObject(
			weapon.ProjectilePrefab,
			Origin.position,
			Quaternion.LookRotation(aimPoint - Origin.position, Vector3.up)
		);
		gameObject.layer = _projectileLayer;
		var psr = gameObject.GetComponent<ProjectileSourceReference>();
        if (psr)
        {
            psr.Source = this;
        }
        var rigidbody = gameObject.GetComponent<Rigidbody>();
		if (rigidbody != null)
        {
			rigidbody.velocity = direction * _projectileSpeed;
        }
	}
}
