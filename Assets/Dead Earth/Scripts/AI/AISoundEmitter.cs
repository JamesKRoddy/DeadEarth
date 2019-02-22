using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISoundEmitter : MonoBehaviour {

    //Inspector Assigned
    [SerializeField] private float _decayRate = 1.0f;

    //Internal
    private SphereCollider _collider = null;
    private float _srcRadius = 0.0f;
    private float _tgtRadius = 0.0f;
    private float _interpolator = 0.0f;
    private float _interolateSpeed = 0.0f;

    // Use this for initialization
    void Start () {
        _collider = GetComponent<SphereCollider>();
        if (!_collider) return;

        //Set start radius
        _srcRadius = _tgtRadius = _collider.radius;

        //Setup Interpolator
        _interpolator = 0.0f;
        if (_decayRate > 0.02f)
            _interolateSpeed = 1.0f / _decayRate;
        else
            _interolateSpeed = 0.0f;
	}

    void FixedUpdate()
    {   if (!_collider) return;
        _interpolator = Mathf.Clamp01(_interpolator + Time.deltaTime*_interolateSpeed);
        _collider.radius = Mathf.Lerp(_srcRadius, _tgtRadius, _interpolator);

        if (_collider.radius < Mathf.Epsilon) _collider.enabled = false;
        else _collider.enabled = true;
    }

    public void SetRadius(float newRadius, bool instantResize = false)
    {
        if (!_collider || newRadius == _tgtRadius) return;
        _srcRadius = (instantResize || newRadius > _collider.radius) ? newRadius : _collider.radius;
        _tgtRadius = newRadius;
        _interpolator = 0.0f;
    }

}
