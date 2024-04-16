using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private Dictionary<string, Queue<GameObject>> _availableObjects;

    public static ObjectPool Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            _availableObjects = new Dictionary<string, Queue<GameObject>>();
        }
        else
        {
            Debug.LogError($"Attempted to make another ObjectPool instance {this.name}!");
            Destroy(this.transform.root.gameObject);                                      
        }
    }

    public GameObject RequestObject(
        GameObject objectPrefab,
        Vector3? desiredPosition = null,
        Quaternion? desiredRotation = null,
        Transform desiredParent = null
    )
    {
        // Adding the instance ID so that the prefabs can (but really shouldn't) have the same name.
        var key = $"{objectPrefab.name} - {objectPrefab.GetInstanceID()}";
        Debug.Log($"Requesting resource {key}.");

        GameObject myObject;
        if (_availableObjects.TryGetValue(key, out var queue) && queue.Count > 0)  // For some reason sometimes this queue can be empty.
        {
            myObject = queue.Dequeue();
            if (queue.Count <= 0)
            {
                _availableObjects.Remove(key);
            }

            myObject.SetActive(true);
        }
        else
        {
            myObject = Instantiate(objectPrefab, desiredParent ?? this.transform);
            myObject.name = key;
        }

        if (desiredPosition != null)
        {
            myObject.transform.position = desiredPosition.Value;
        }
        if (desiredRotation != null)
        {
            myObject.transform.rotation = desiredRotation.Value;
        }

        return myObject;
    }

    // You should only call ReleaseObject on GameObjects created using RequestObject.
    public void ReleaseObject(GameObject myObject)
    {
        Debug.Log($"Releasing resource {myObject.name}.");

        if (!_availableObjects.TryGetValue(myObject.name, out var queue))
        {
            // TODO: Consider visually representing this by creating a root GameObject for all instances of this prefab.
            queue = new Queue<GameObject>();
            _availableObjects.Add(myObject.name, queue);
        }
        myObject.SetActive(false);
        queue.Enqueue(myObject);
    }

    public void DoCleanup()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _availableObjects.Clear();
    }
}
