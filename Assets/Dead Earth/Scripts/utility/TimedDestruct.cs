using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestruct : MonoBehaviour {

    [SerializeField] private float _time = 10.0f;

    void Awake()
    {
        Destroy(gameObject, _time);
    }
}
