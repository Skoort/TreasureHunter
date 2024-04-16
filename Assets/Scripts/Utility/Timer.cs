using UnityEngine;
using UnityEngine.Events;

// This timer works while the gameobject is active in the hierarchy
public class Timer : MonoBehaviour
{
    [SerializeField]
    private UnityAction _onTimer = default;

    [SerializeField]
    private float _duration = 10;
    private float _countdown;

    [SerializeField]
    private bool _isPausedOnAwake = default;

    private bool _isPaused;

    private void Awake()
    {
        _countdown = _duration;
        if (_isPausedOnAwake)
        {
            Pause();
        }
        else
        {
            Unpause();
        }
    }

    //private void OnEnable()
    //{
    //    Unpause();
    //}

    //private void OnDisable()
    //{
    //    Pause();
    //}

    public void Pause()
    {
        _isPaused = true;
        if (enabled)
        {
            enabled = false;
        }
    }

    public void Unpause()
    {
        _isPaused = false;
        if (!enabled)
        {
            enabled = true;
        }
    }

    public void ResetTimer(float newDuration = -1)
    {
        if (newDuration >= 0)
        {
            _duration = newDuration;
        }
        _countdown = _duration;
    }

    private void Update()
    {
        _countdown -= Time.deltaTime;
        if (_countdown <= 0)
        {
            _countdown = 0;
            Pause();
            _onTimer?.Invoke();
        }
    }
}
