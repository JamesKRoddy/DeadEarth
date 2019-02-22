using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentNoRootMotion : MonoBehaviour {

    //Assigned in inspector
    public AIWaypointNetwork waypointNetwork = null;
    public int currentIndex = 0;
    public bool hasPath = false;
    public bool pathPending = false;
    public bool pathStale = false;
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
    public AnimationCurve jumpCurve = new AnimationCurve();

    private NavMeshAgent _navAgent = null;
    private Animator _animator = null;
    private float _originalMaxSpeed = 0;

	// Use this for initialization
	void Start () {
        _navAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (_navAgent)
        {
            _originalMaxSpeed = _navAgent.speed;
        }

        if(waypointNetwork == null)
        {
            return;
        }

        SetNextDestination(false);
	}
	
    void SetNextDestination (bool increment)
    {
        if (!waypointNetwork) return;

        //if increment is ture sent incStep to 1 else 0
        int incStep = increment ? 1 : 0;
        Transform nextWaypointTransform = null;

        int nextWaypoint = (currentIndex + incStep >= waypointNetwork.Waypoints.Count) ? 0 : currentIndex + incStep;
        nextWaypointTransform = waypointNetwork.Waypoints[nextWaypoint];

        if (nextWaypointTransform != null)
        {
            currentIndex = nextWaypoint;
            _navAgent.destination = nextWaypointTransform.position;
            return;
        }        

        currentIndex++;
    }

	// Update is called once per frame
	void Update () {

        int turnOnSpot;

        hasPath = _navAgent.hasPath;
        pathPending = _navAgent.pathPending;
        pathStale = _navAgent.isPathStale;
        pathStatus = _navAgent.pathStatus;

        Vector3 cross = Vector3.Cross(transform.forward, _navAgent.desiredVelocity.normalized);
        float horizontal = (cross.y < 0) ? -cross.magnitude : cross.magnitude; // figuring out if its a left or right turn
        horizontal = Mathf.Clamp(horizontal * 2.32f, -2.32F, 2.32f);

        if(_navAgent.desiredVelocity.magnitude< 0.75f && Vector3.Angle(transform.forward, _navAgent.desiredVelocity) > 10.0f)
        {
            _navAgent.speed = 0.1f;
            turnOnSpot = (int)Mathf.Sign(horizontal);
        }
        else
        {
            _navAgent.speed = _originalMaxSpeed;
            turnOnSpot = 0;
        }
        
        _animator.SetFloat("horizontal", horizontal, 0.1f, Time.deltaTime);
        _animator.SetFloat("vertical", _navAgent.desiredVelocity.magnitude, 0.1f, Time.deltaTime);
        _animator.SetInteger("turnOnSpot", turnOnSpot );


        //if (_navAgent.isOnOffMeshLink)
        //{
        //    StartCoroutine(Jump(1.0f));
        //    return;
        //}

        if((_navAgent.remainingDistance<=_navAgent.stoppingDistance && !pathPending) || (pathStatus == NavMeshPathStatus.PathInvalid))
        {
            SetNextDestination(true);
        } else if (_navAgent.isPathStale)
        {
            SetNextDestination(false);
        }
	}

    IEnumerator Jump(float duration)
    {
        OffMeshLinkData data = _navAgent.currentOffMeshLinkData;
        Vector3 startPos = _navAgent.transform.position;
        Vector3 endPos = data.endPos + (_navAgent.baseOffset * Vector3.up);
        float time = 0.0f;

        while (time <= duration)
        {
            float t = time / duration;
            _navAgent.transform.position = Vector3.Lerp(startPos, endPos, t) + jumpCurve.Evaluate(t) * Vector3.up;
            time += Time.deltaTime;
            yield return null;
        }

        _navAgent.CompleteOffMeshLink();
    }
}
