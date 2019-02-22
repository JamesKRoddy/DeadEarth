using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class NavAgentRootMotion : MonoBehaviour {

    //Assigned in inspector
    public AIWaypointNetwork waypointNetwork = null;
    public int currentIndex = 0;
    public bool hasPath = false;
    public bool pathPending = false;
    public bool pathStale = false;
    public NavMeshPathStatus pathStatus = NavMeshPathStatus.PathInvalid;
    public AnimationCurve jumpCurve = new AnimationCurve();
    public bool mixedMode = true;

    private NavMeshAgent _navAgent = null;
    private Animator _animator = null;
    private float _smoothAngle = 0.0f;

	// Use this for initialization
	void Start () {
        _navAgent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        _navAgent.updateRotation = false;

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

        hasPath = _navAgent.hasPath;
        pathPending = _navAgent.pathPending;
        pathStale = _navAgent.isPathStale;
        pathStatus = _navAgent.pathStatus;

        //calculate speed and rotation from nav agent to send into animator NB make sure to apply stopping distance in inspector
        Vector3 localDesiredVelocity = transform.InverseTransformVector(_navAgent.desiredVelocity);
        float angle = Mathf.Atan2(localDesiredVelocity.x, localDesiredVelocity.z) * Mathf.Rad2Deg; //coverted to degrees
        _smoothAngle = Mathf.MoveTowardsAngle(_smoothAngle, angle, 80.0f * Time.deltaTime); //unable to move more than 80 degrees in a single second

        float speed = localDesiredVelocity.z; //how fast walking in forward direction

        _animator.SetFloat("angle", _smoothAngle);
        _animator.SetFloat("speed", speed, 0.1f, Time.deltaTime);

        if (_navAgent.desiredVelocity.sqrMagnitude > Mathf.Epsilon)//very small float
        {
            if (!mixedMode || (mixedMode && Mathf.Abs(angle) < 80.0f && _animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion")))
            {
                Quaternion lookRotation = Quaternion.LookRotation(_navAgent.desiredVelocity, Vector3.up);
                //smooth desired velocity
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5.0f * Time.deltaTime);
            }
        }



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

    private void OnAnimatorMove()
    {
        //rotation driven by animation
        if (mixedMode && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Locomotion"))//if we are in any state but locomotion we want rootmotion
        {
            transform.rotation = _animator.rootRotation;
        }

        _navAgent.velocity = _animator.deltaPosition / Time.deltaTime;
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
