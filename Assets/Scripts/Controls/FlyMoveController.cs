using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyMoveController : MonoBehaviour
{
	[SerializeField] private float _moveSpeed = 3;

	private Rigidbody _rb;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
	}

	private	void Update()
	{
		var moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		var moveDir = Vector3.up * moveInput.y + Vector3.right * moveInput.x;
		if (moveDir.sqrMagnitude > 1)
		{
			moveDir.Normalize();
		}

		_rb.velocity = moveDir * (_moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? 2.5F : 1));
	}
}