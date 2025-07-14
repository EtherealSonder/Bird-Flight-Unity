using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new();

    private static UnityMainThreadDispatcher _instance = null;
    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
        return _instance;
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
                _executionQueue.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}
