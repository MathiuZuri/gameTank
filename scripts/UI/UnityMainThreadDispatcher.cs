using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Una versión hilo-segura (thread-safe) de un Dispatcher para Unity.
/// Permite ejecutar acciones en el hilo principal desde hilos secundarios (como los de Firebase).
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            // Busca si ya existe en la escena
            _instance = FindFirstObjectByType<UnityMainThreadDispatcher>();

            // Si no existe, créalo automáticamente
            if (!_instance)
            {
                var obj = new GameObject("UnityMainThreadDispatcher");
                _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj); // Hacerlo inmortal entre escenas
            }
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // Evitar duplicados
        }
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Encola una acción para ser ejecutada en el hilo principal (Main Thread).
    /// </summary>
    /// <param name="action">La función a ejecutar (ej: actualizar UI)</param>
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Encola una Corrutina para ser ejecutada en el hilo principal.
    /// </summary>
    public void Enqueue(IEnumerator action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => {
                StartCoroutine(action);
            });
        }
    }
}