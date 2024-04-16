using System.Collections;
using UnityEngine;

public class Grub : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1;

    [SerializeField] private Transform _feeler = default;
    // TODO: Currently the feeler is assumed to emerge from the horizontal center of the grub. So it has to be equal to the half-width of the grub.
    [SerializeField] private float _wallFeelDistance = 0;
    [SerializeField] private float _holeFeelDistance = 0;


    [SerializeField] private Rigidbody _rb = default;
    [SerializeField] private SpriteRenderer _spriteRenderer = default;

    // TODO: This will probably be extended to all "Chunk Objects" as well.
    private int _layerMask;

    private float _yVelocity = 0;
    private bool _isGrounded = false;
    private bool _isMoving = false;
    private int _direction = 1;
    [SerializeField]
    private bool _hasWall = false;
    [SerializeField]
    private bool _hasHole = false;
    private float _distanceToObstacle = 0;
    private bool _isInTransition = false;
    private Coroutine _currentCoroutine = null;

    [SerializeField, Tooltip("The chance for the grub to flip direction after moving for 1 second.")]
    private float _chanceToChangeDirection = 0.2F;
    [SerializeField, Tooltip("The chance for the grub to stop moving after moving for 1 second.")]
    private float _chanceToStopMoving = 0.333F;
    [SerializeField, Tooltip("The chance for the grub to start moving after not moving for 1 second.")]
    private float _chanceToStartMoving = 0.5F;
    [SerializeField]
    private float _decisionInterval = 0.2F;
    private float _decisionTimer = 0;

    private void Awake()
    {
        _layerMask = 1 << LayerMask.NameToLayer("Chunk");
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        _decisionTimer -= Time.deltaTime;

        FeelForGround();
        FeelForWall();
        FeelForHole();
        FeelForHoleWall();

        if (!_isInTransition)
        {
            DecideAction();

            if (!_isGrounded)
            {
                _yVelocity -= Time.deltaTime * 9.81F;
                _rb.position += _feeler.up * _yVelocity * Time.deltaTime;
            } else
            if (_isMoving)
            {
                _yVelocity = 0;
                _rb.position += _feeler.forward * _direction * _moveSpeed * Time.deltaTime;
            }

            if (_isMoving && _hasWall)
            {
                TryToStopCurrentTransition();
                _currentCoroutine = StartCoroutine(IE_DoWallTransition());
            } else
            if (_isMoving && _hasHole)
            {
                TryToStopCurrentTransition();
                _currentCoroutine = StartCoroutine(IE_DoHoleTransition());
            }
        }
    }

    private void DecideAction()
    {
        if (!_isGrounded)
        {
            return;
        }

        if (_decisionTimer <= 0)
        {
            _decisionTimer = _decisionInterval;

            if (Random.value < _chanceToChangeDirection * _decisionInterval)
            {
                FlipDirection();
                return;
            }

            if (_isMoving)
            {
                if (Random.value < _chanceToStopMoving * _decisionInterval)
                {
                    _isMoving = false;
                    return;
                }
            }
            else
            {
                if (Random.value < _chanceToStartMoving * _decisionInterval)
                {
                    _isMoving = true;
                    return;
                }
            }
        }
    }

    private void FeelForGround()
    {
        // Try to see if the wall has disappeared during a transition.
        if (!Physics.CheckSphere(transform.position, 0.2F, _layerMask, QueryTriggerInteraction.Ignore))
        {
            TryToStopCurrentTransition();
        }

        if (_isInTransition)
        {
            return; // This part snaps the grub to the ground.
        }

        if (Physics.Raycast(transform.position + transform.up * 0.25F, -transform.up, out var hitInfo, 0.30F, _layerMask, QueryTriggerInteraction.Ignore))
        {
            _isGrounded = true;
            transform.rotation = Quaternion.LookRotation(transform.forward, hitInfo.normal);
            transform.position = hitInfo.point;
            _yVelocity = 0;
        }
        else
        {
            _isGrounded = false;
            transform.rotation = Quaternion.identity;
        }
    }

    private void FeelForWall()
    {
        if (Physics.Raycast(_feeler.position, _feeler.forward * _direction, out var hitInfo, _wallFeelDistance + 0.05F, _layerMask, QueryTriggerInteraction.Ignore))
        {
            _hasWall = true;
            _distanceToObstacle = Vector3.Distance(_feeler.position, hitInfo.point);
        }
        else
        {
            _hasWall = false;
        }
    }

    private void FeelForHole()
    {
        var offsetStart = _feeler.position + _feeler.forward * (_direction * _holeFeelDistance + 0.05F);
        var feelerHeight = _feeler.localPosition.y;
        if (Physics.Raycast(offsetStart, -_feeler.up, out var _, feelerHeight + 0.05F, _layerMask, QueryTriggerInteraction.Ignore))
        {
            FeelForHoleWall();
        }
        else
        {
            _hasHole = false;
        }
    }

    private void FeelForHoleWall()
    {
        var feelerHeight = _feeler.localPosition.y;
        var offsetStart = _feeler.position + _feeler.forward * (_direction * _holeFeelDistance + 0.05F) - _feeler.up * (feelerHeight + 0.05F);
        if (Physics.Raycast(offsetStart, _feeler.forward * -_direction, out var hitInfo, 1, _layerMask, QueryTriggerInteraction.Ignore))
        {
            _hasHole = true;
            _distanceToObstacle = Vector3.Distance(_feeler.position, hitInfo.point + _feeler.up * (feelerHeight + 0.05F));
        }
        else
        {
            _hasHole = false;
        }
    }

    private void TryToStopCurrentTransition()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _isInTransition = false;
        }
    }

    private IEnumerator IE_DoWallTransition()
    {
        _isInTransition = true;

        var radius = _wallFeelDistance;
        var progress = radius - _distanceToObstacle;
        var circleCenter = transform.position - transform.right * _direction * progress + transform.up * radius;

        progress = Mathf.Clamp(progress, 0, radius);

        var theta = Mathf.Atan2(progress, radius);

        var angularSpeed = _moveSpeed / radius;

        var up = transform.up;
        var right = transform.right * _direction;

        do
        {
            yield return null;  // This is before for easier debugging.
            var delta = angularSpeed * Time.deltaTime;
            theta += delta;
            transform.position = circleCenter + right * (Mathf.Sin(theta) * radius) + up * (-Mathf.Cos(theta) * radius);
            transform.Rotate(0, 0, delta * +_direction * Mathf.Rad2Deg);
        }
        while (theta < 90 * Mathf.Deg2Rad);

        _isInTransition = false;
    }

    private IEnumerator IE_DoHoleTransition()
    {
        _isInTransition = true;

        var radius = _holeFeelDistance;
        var progress = radius - _distanceToObstacle;
        var circleCenter = transform.position - transform.right * _direction * progress - transform.up * radius;

        progress = Mathf.Clamp(progress, 0, radius);

        var theta = Mathf.Atan2(progress, radius);

        var angularSpeed = _moveSpeed / radius;

        var up = transform.up;
        var right = transform.right * _direction;

        do
        {
            yield return null;  // This is before for easier debugging.
            var delta = angularSpeed * Time.deltaTime;
            theta += delta;
            transform.position = circleCenter + right * (Mathf.Sin(theta) * radius) + up * (+Mathf.Cos(theta) * radius);
            transform.Rotate(0, 0, delta * -_direction * Mathf.Rad2Deg);
        }
        while (theta < 90 * Mathf.Deg2Rad);

        _isInTransition = false;
    }

    private void FlipDirection()
    {
        _direction = -_direction;
        _spriteRenderer.flipX = !_spriteRenderer.flipX;
    }

    private void OnDrawGizmos()
    {
        var heightOffset = _feeler.up * 0.025F;
        var line1Start = _feeler.position + heightOffset;
        var line1End = line1Start + _feeler.forward * (_direction * _wallFeelDistance + _direction * 0.05F);
        Gizmos.color = _hasWall ? Color.red : Color.blue;
        Gizmos.DrawLine(line1Start, line1End);

        var line2Start = _feeler.position - heightOffset;
        var line2End = line2Start + _feeler.forward * (_direction * _holeFeelDistance + _direction * 0.05F);
        var line3Start = line2End;
        var line3End = line3Start - _feeler.up * (_feeler.localPosition.y + 0.05F) + heightOffset;
        Gizmos.color = _hasHole ? Color.red : Color.blue;
        Gizmos.DrawLine(line2Start, line2End);
        Gizmos.DrawLine(line3Start, line3End);

        if (_hasWall || _hasHole)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_feeler.position, _feeler.position + _feeler.forward * _direction * _distanceToObstacle);
        }
    }
}
