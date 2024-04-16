using UnityEngine;

public class LookController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _playerRenderer = default;
    [SerializeField] private SpriteRenderer _gunRenderer = default;

    [SerializeField] private Transform _gunRoot = default;
    [SerializeField] private Transform _lookLeftGunRoot = default;
    [SerializeField] private Transform _lookRightGunRoot = default;

    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        var lookPoint = _camera.ScreenToWorldPoint(Input.mousePosition);

        var parent = (lookPoint.x >= transform.position.x)
            ? _lookRightGunRoot
            : _lookLeftGunRoot;
        _gunRoot.SetParent(parent, false);

        var lookDir = lookPoint - transform.position;
        lookDir.z = 0;

        _gunRoot.right = lookDir.normalized;
        _gunRenderer.flipY = lookDir.x < 0;
        _playerRenderer.flipX = lookDir.x < 0;
    }
}
