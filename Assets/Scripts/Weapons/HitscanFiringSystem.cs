using UnityEngine;

public class HitscanFiringSystem : FiringSystem
{
	[SerializeField] protected LayerMask HitLayerMask = default;

	protected override void DoFire()
	{
		var direction = Target == null
			? Origin.forward
			: (Target.position - Origin.position).normalized;
		var aimPoint = direction * 100 + Origin.position + Random.insideUnitSphere * Weapon.Spread;

		var ray = new Ray(Origin.position, aimPoint - Origin.position);
		if (Physics.Raycast(ray, out var hitInfo, Weapon.Range, HitLayerMask.value))
		{
			Debug.Log($"Hit {hitInfo.transform.name}!");
			Debug.DrawLine(ray.origin, hitInfo.point, Color.red, 0.1F);
		}
		else
		{
			Debug.DrawLine(ray.origin, ray.direction.normalized * Weapon.Range, Color.blue, 0.1F);
		}
	}
}
