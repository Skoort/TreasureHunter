using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveController : MonoBehaviour
{
	[SerializeField] private float _moveSpeed = 3F;
	[SerializeField] private float _jumpSpeed = 10F;

	[SerializeField]
	private int _numTestRays = 3;
	[SerializeField, Tooltip("How deep inside the collider to start test rays.")]
	private float _testRayBuffer = 0.05F;
	[SerializeField, Tooltip("How long test rays for walls/floor should be.")]
	private float _testRayDepth = 0.25F;

	private Rigidbody _rb;
	private float _width;
	private float _height;

	private float _horzInput = 0;
	private float _direction = +1;
	private bool _isGrounded;
	private bool _isJumping;

	private bool _jumpQueued;
	private bool _moveQueued;
	[SerializeField, Tooltip("Controls how much leeway there is for mistimed jumps.")]
	private float _jumpDelayBuffer = 0.2F;
	private float _jumpDelayTimer;
	private float _timeLastGrounded;

	private int _chunkLayer;
	private int _chunkLayerMask;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();

		_chunkLayer = LayerMask.NameToLayer("Chunk");
		_chunkLayerMask = 1 << _chunkLayer;
	}

    private void Start()
    {
		var collider = GetComponentInChildren<Collider>();
		var extents = collider.bounds.extents;
		_width = extents.x * 2;
		_height = extents.y * 2;
    }

    private	void Update()
	{
		// Handle move queueing.
		_horzInput = Input.GetAxis("Horizontal");
		if (_horzInput != 0)
        {
			_direction = Mathf.Sign(_horzInput);
		}
		_moveQueued = Mathf.Abs(_horzInput) > 0.1F;


		// Handle jump queueing.
		if (Input.GetButtonDown("Jump"))
        {
			_jumpQueued = true;
			_jumpDelayTimer = 0;
        }
		if (_jumpQueued)
        {
			_jumpDelayTimer += Time.deltaTime;
			if (_jumpDelayTimer >= _jumpDelayBuffer)
            {
				_jumpQueued = false;
				_jumpDelayTimer = 0;
            }
        }
	}

    private void FixedUpdate()
    {
		CheckGroundedness();
		CheckIfMovementBlocked();

		float horzSpeed = _rb.velocity.x;
		float vertSpeed = _rb.velocity.y;

		//// Can only control the character while grounded.
		//if (_isGrounded)
		//{
		// TODO: Implement drag if the key is no longer pressed while in air.
		if (_moveQueued)
		{
			var sprintMod = Input.GetKey(KeyCode.LeftShift) ? 2.5F : 1;
			horzSpeed = _horzInput * _moveSpeed * sprintMod;
		}
		//}

		if (_jumpQueued && Time.time - _timeLastGrounded <= _jumpDelayBuffer)
		{
			// Adjust physics state.
			vertSpeed = _jumpSpeed;

			// Adjust logical state. While in the jumping state, ground checks
			// do not occur, so manually set _isGrounded to false.
			_isJumping = true;
			_isGrounded = false;

			// Unqueue the jump.
			_jumpQueued = false;
		}

		_rb.velocity = new Vector3(horzSpeed, vertSpeed);

		if (_isJumping)
        {
			// TODO: This check won't work anymore when we add knockback while airborn.
			if (_rb.velocity.y < 0)
            {
				_isJumping = false;
            }
        }
    }

    private void CheckGroundedness()
    {
		if (_isJumping)
        {
            return;  // Jumping temporarily disables ground checks.
        }

		foreach (var ray in GetGroundRays())
        {
			if (Physics.Raycast(ray.origin, ray.direction, _testRayDepth, _chunkLayerMask))
			{
				_isGrounded = true;
				_timeLastGrounded = Time.time;
				return;
			}
		}
        _isGrounded = false;
    }

	private IEnumerable<Ray> GetGroundRays()
    {
		for (int i = 0; i < _numTestRays; ++i)
		{
			var origin = _rb.position + new Vector3(
				_width * (i / (float)(_numTestRays - 1)) - _width * 0.5F,
				_testRayBuffer
			);
			yield return new Ray(origin, Vector3.down);
		}
	}

	private void CheckIfMovementBlocked()
    {
		if (!_moveQueued)
        {
			return;  // Nothing to do.
        }

		foreach (var ray in GetWallRays())
        {
			if (Physics.Raycast(ray.origin, ray.direction, _testRayDepth, _chunkLayerMask))
			{
				_moveQueued = false;
				return;
			}
		}
	}

	private IEnumerable<Ray> GetWallRays()
    {
		for (int i = 0; i < _numTestRays; ++i)
		{
			var origin = _rb.position + new Vector3(
				_direction * _width * 0.5F - _direction * _testRayBuffer,
				_height * (i / (float)(_numTestRays - 1))
			);
			yield return new Ray(origin, new Vector3(_direction, 0));
		}
	}

    private void OnDrawGizmos()
    {
		if (!Application.isPlaying)
        {
			// Awake/Start weren't called.
			var collider = GetComponentInChildren<Collider>();
			var extents = collider.bounds.extents;
			_width = extents.x * 2;
			_height = extents.y * 2;
			_rb = GetComponent<Rigidbody>();
		}

		// Draw floor feelers.
		Gizmos.color = Color.green;
		foreach (var ray in GetGroundRays())
        {
			Gizmos.DrawLine(ray.origin, ray.GetPoint(_testRayDepth));
        }

		// Draw wall feelers.
		Gizmos.color = Color.red;
		foreach (var ray in GetWallRays())
		{
			Gizmos.DrawLine(ray.origin, ray.GetPoint(_testRayDepth));
		}
	}
}