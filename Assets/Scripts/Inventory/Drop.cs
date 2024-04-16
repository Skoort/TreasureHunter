using UnityEngine;

// The creation and destruction of this object is handled by DropManager.
public class Drop : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private SpriteRenderer _renderer;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
    }

    private bool _isTriggered;
    private ItemData _item;

    public void AssignItem(ItemData item)
    {
        _isTriggered = false;
        _item = item;
        _renderer.sprite = item.Sprite;
        _rigidbody.velocity = Random.insideUnitCircle * 2;
    }

    public void OnTriggerEnter(Collider other)
    //public void OnCollisionEnter(Collision other)
    {
        Debug.Log("Collided with something!");
        // Prevent the drop from potentially being picked up multiple times.
        if (_isTriggered)
        {
            return;
        }

        var root = other.transform.root;
        if (root.tag == "Player")
        {
            Debug.Log("Collided with player!");
            var inventory = root.GetComponentInChildren<Inventory>();
            if (inventory)
            {
                Debug.Log("Collided with inventory!");
                _isTriggered = true;
                inventory.Add(_item, 1);
                DropManager.Instance.DespawnDrop(this);
            }
        }
    }

    // TODO: Add magnet property.
}
