using System;
using UnityEngine;

public enum GunState
{
    IDLE,
    SHOOTING,
    RELOADING,
    LOCKED
}

public abstract class FiringSystem : MonoBehaviour
{
    [SerializeField] public bool IsEquipped = default;  // Determines if the weapon is physically on the character, as opposed to able to be picked up.

    [SerializeField] protected Transform Target = default;  // If the Target is null, shoot straight forward.

    [SerializeField] protected Transform Origin = default;

    [SerializeField] private RangedWeapon _weapon = default;
    public RangedWeapon Weapon => _weapon;

    public event Action OnFire;
    public event Action OnOutOfAmmo;
    public event Action OnReloaded;

    public GunState State { get; protected set; }

    public int Ammo { get; protected set; }

    float _shotReadyProgress = 0;
    float _reloadProgress = 0;

    [SerializeField] private float _nextShotInputLeeway = 0;
    private bool _isSingleShotQueued;

    public void Fire()
    {
        if (State != GunState.IDLE)
        {
            return;
        }

        State = GunState.SHOOTING;

        TryToFire();  // Always fire the first shot during the first frame, unless the weapon is not ready to fire yet.

        if (!_weapon.IsAutomatic)
        {
            if ((1 - _shotReadyProgress) <= _nextShotInputLeeway)
            {
                _isSingleShotQueued = true;
            }
            else
            {
                StopFiring();
            }
        }
    }

    // Use this method to stop firing an automatic weapon.
    public virtual void StopFiring()
    {
        if (State == GunState.SHOOTING)
        {
            State = GunState.IDLE;
        }
    }

    protected void TryToFire()
    {
        if (State != GunState.SHOOTING)
        {
            return;
        }

        if (_shotReadyProgress < 1)
        {
            return;
        }

        if (Ammo < _weapon.AmmoPerShot)
        {
            StopFiring();
            OnOutOfAmmo?.Invoke();
            return;
        }

        DoFire();
        OnFire?.Invoke();

        Ammo -= _weapon.AmmoPerShot;
        _shotReadyProgress -= 1;

        if (_isSingleShotQueued)
        {
            _isSingleShotQueued = false;
            StopFiring();
        }
    }

    protected abstract void DoFire();

    public void Reload()
    {
        State = GunState.RELOADING;
        StopFiring();
    }

    private void HandleReload(float dt)
    {
        if (State == GunState.RELOADING)
        {
            _reloadProgress += dt / _weapon.ReloadTime;
            if (_reloadProgress >= 1)
            {
                _reloadProgress = 0;
                State = GunState.IDLE;
                Ammo = Weapon.MaxAmmo;
                OnReloaded?.Invoke();
            }
        }
    }

    private void Update()
    {
        if (State == GunState.LOCKED)
        {
            return;
        }

        var dt = Time.deltaTime;

        _shotReadyProgress += dt * _weapon.RateOfFire;
        if (_shotReadyProgress > 1)
        {
            _shotReadyProgress = 1;
        }

        TryToFire();

        HandleReload(dt);
    }

    protected virtual void Awake()
    {
        Ammo = _weapon.MaxAmmo;
    }

    protected virtual void OnEnable()
    {
        State = GunState.IDLE;
        _shotReadyProgress = 1;
    }

    protected virtual void OnDisable()
    {
        StopFiring();
    }

    private int _lockCount = 0;

    public void Lock()
    {
        State = GunState.LOCKED;
        ++_lockCount;
    }

    public void Unlock()
    {
        --_lockCount;
        if (_lockCount <= 0)
        {
            State = GunState.IDLE;
        }
    }
}
