using UnityEngine;

public abstract class RangedWeapon : Weapon
{
	public int MaxAmmo => _maxAmmo;
	public int AmmoPerShot => _ammoPerShot;
	public float ReloadTime => _reloadTime;
	public bool IsAutomatic => _isAutomatic;
	public float RateOfFire => _rateOfFire;
	public float Range => _range;
	public float Spread => _spread;
	public float Recoil => _recoil;

	public float HeatSpeed => _heatSpeed;
	public float CoolSpeed => _coolSpeed;
	public float OverheatDuration => _overheatDuration;

	public float JamChancePerShot => _jamChancePerShot;
	public float JamDuration => _jamDuration;

	[SerializeField] private int _maxAmmo = 30;
	[SerializeField] private int _ammoPerShot = 1;
	[SerializeField] private float _reloadTime = 2;
	[SerializeField] private bool _isAutomatic = false;
	[SerializeField] private float _rateOfFire = 1;
	[SerializeField] private float _range = 30;
	[SerializeField] private float _spread = 2;
	[SerializeField] private float _recoil = 1;

	[Header("Weapon Overheat Information"), Tooltip("Zero out if this weapon doesn't overheat.")]
	[SerializeField] private float _heatSpeed = 0.1F;
	[SerializeField] private float _coolSpeed = 0.2F;
	[SerializeField] private float _overheatDuration = 1;

	[Header("Weapon Jam Information"), Tooltip("Zero out if this weapon doesn't jam.")]
	[SerializeField] private float _jamChancePerShot = 0.01F;
	[SerializeField] private float _jamDuration = 1;
}
