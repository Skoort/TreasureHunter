using UnityEngine;

public class ConstVelocity : MonoBehaviour
{
    [SerializeField] private Vector3 _velocity = default;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        _rigidbody.velocity = _velocity + new Vector3(0, _rigidbody.velocity.y, 0);
    }
}
