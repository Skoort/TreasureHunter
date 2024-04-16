using UnityEngine;

public abstract class ProjectileSourceReference : MonoBehaviour
{
    [field: SerializeField]
    public ProjectileFiringSystem Source { get; set; }
}
