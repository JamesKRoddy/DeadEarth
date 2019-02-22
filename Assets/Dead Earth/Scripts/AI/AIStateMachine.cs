using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AIStateType { NONE, IDLE, ALERTED, PATROL, ATTACK, FEEDING, PURSUIT, DEAD }
public enum AITargetType { NONE, WAYPOINT, VISUAL_PLAYER, VISUAL_LIGHT, VISUAL_FOOD, AUDIO }
public enum AITriggerEventType { ENTER, STAY, EXIT }
public enum AIBoneAlignmentType { XAXIS, YAXIS, ZAXIS, XAXISINVERTED, YAXISINVERTED, ZAXISINVERTED }

public struct AITarget
{
    private AITargetType _type;
    private Collider _collider;
    private Vector3 _position;
    private float _distance;
    private float _time;

    public AITargetType type { get { return _type; } }
    public Collider collider { get { return _collider; } }
    public Vector3 position { get { return _position; } }
    public float distance { get { return _distance; } set { _distance = value; } }
    public float time { get { return _time; } }

    public void Set(AITargetType t, Collider c, Vector3 p, float d)
    {
        _type = t;
        _collider = c;
        _position = p;
        _distance = d;
        _time = Time.time;
    }

    public void Clear()
    {
        _type = AITargetType.NONE;
        _collider = null;
        _position = Vector3.zero;
        _distance = Mathf.Infinity;
        _time = 0.0f;
    }
}

public abstract class AIStateMachine : MonoBehaviour {

    //Public
    public AITarget visualThreat = new AITarget();
    public AITarget audioThreat = new AITarget();

    //Protected
    protected AIState _currentState = null;
    protected Dictionary<AIStateType, AIState> _states = new Dictionary<AIStateType, AIState>();
    protected AITarget _target = new AITarget();
    protected int _rootPositionRefCount = 0;
    protected int _rootRotationRefCount = 0;
    protected bool _isTargetReached = false;
    protected List<Rigidbody> _bodyParts = new List<Rigidbody>();
    protected int _aiBodyPartLayer = -1;
    protected bool _cinematicEnabled = false;

    //Protected Inspector Assigned
    [SerializeField] protected AIStateType _currentStateType = AIStateType.IDLE;
    [SerializeField] protected Transform _rootBone = null;
    [SerializeField] protected AIBoneAlignmentType _rootBoneAlignment = AIBoneAlignmentType.ZAXIS;
    [SerializeField] protected SphereCollider _targetTrigger = null;
    [SerializeField] protected SphereCollider _sensorTrigger = null;
    [SerializeField] [Range(0, 15)] protected float _stoppingDistance = 1.0f; //range is slider bar
    [SerializeField] protected AIWaypointNetwork _wayPointNetwork = null;
    [SerializeField] protected bool _randomPatrol = false;
    [SerializeField] protected int _currentWaypint = -1;

    //Component Cache
    protected Animator _animator = null;
    protected NavMeshAgent _navAgent = null;
    protected Collider _collider = null;
    protected Transform _transform = null;

    //Public Properties
    public bool isTargetReached { get { return _isTargetReached; } }
    public bool inMeleeRange { get; set; }
    public Animator animator { get { return _animator; } }
    public NavMeshAgent navAgent { get { return _navAgent; } }
    public Vector3 sensorPosition
    {
        get
        {
            if (_sensorTrigger == null) return Vector3.zero;
            Vector3 point = _sensorTrigger.transform.position;
            point.x += _sensorTrigger.center.x * _sensorTrigger.transform.lossyScale.x;
            point.y += _sensorTrigger.center.y * _sensorTrigger.transform.lossyScale.y;
            point.z += _sensorTrigger.center.z * _sensorTrigger.transform.lossyScale.z;
            return point;
        }
    }

    public bool useRootPostion { get { return _rootPositionRefCount>0; } }
    public bool useRootRotation { get { return _rootRotationRefCount>0; } }

    public float sensorRadius
    {
        get
        {
            if (_sensorTrigger == null) return 0.0f;
            float raduis = Mathf.Max(_sensorTrigger.radius * _sensorTrigger.transform.lossyScale.x, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.y);

            return Mathf.Max(raduis, _sensorTrigger.radius * _sensorTrigger.transform.lossyScale.z);
        }
    }

    public AITargetType targetType { get { return _target.type; } }
    public Vector3 targetPosition { get { return _target.position; } }
    public int targetColliderID
    {
        get
        {
            if (_target.collider)
                return _target.collider.GetInstanceID();
            else
                return -1;
        }
    }
    public bool cinematicEnabled
    {
        get { return _cinematicEnabled; }
        set { _cinematicEnabled = value; }
    }


    //Functions

    protected virtual void Awake()
    {
        //cache all frequently used components
        _transform = transform;
        _animator = GetComponent<Animator>();
        _navAgent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();

        //get body part layer
        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

        //check for a valid scenemanager
        if(GameSceneManager.instance != null)
        {
            if (_collider) GameSceneManager.instance.RegisterAIStateMachine(_collider.GetInstanceID(), this);
            if (_sensorTrigger) GameSceneManager.instance.RegisterAIStateMachine(_sensorTrigger.GetInstanceID(), this);
        }

        //get rbs for ragdolling
        if(_rootBone != null){
            Rigidbody[] bodies = _rootBone.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody bodyPart in bodies)
            {
                if(bodyPart != null && bodyPart.gameObject.layer == _aiBodyPartLayer)
                {
                    _bodyParts.Add(bodyPart);
                    GameSceneManager.instance.RegisterAIStateMachine(bodyPart.GetInstanceID(), this);
                }
            }
        }
    }

    protected virtual void Start()
    {
        if (_sensorTrigger != null)
        {
            AISensor script = _sensorTrigger.GetComponent<AISensor>();
            if (script != null)
            {
                script.parentStateMachine = this;
            }
        }

        AIState[] states = GetComponents<AIState>();
        
        foreach(AIState state in states)
        {
            if(state!=null && !_states.ContainsKey(state.GetStateType()))
            {
                _states[state.GetStateType()] = state;
                state.SetStateMachine(this);
            }
        }

        if (_states.ContainsKey(_currentStateType))
        {
            _currentState = _states[_currentStateType];
            _currentState.OnEnterState();
        }
        else
        {
            _currentState = null;
        }

        if (_animator)
        {
            AIStateMachineLink[] scripts = _animator.GetBehaviours<AIStateMachineLink>();
            foreach(AIStateMachineLink script in scripts)
            {
                script.stateMachine = this;
            }
        }
    }

    public Vector3 GetWaypointPosition (bool increment)
    {
        //first time called
        if (_currentWaypint == -1)
        {
            if (_randomPatrol)
                _currentWaypint = Random.Range(0, _wayPointNetwork.Waypoints.Count);
            else
                _currentWaypint = 0;
        }
        else
        if (increment)
           NextWaypoint();

        if (_wayPointNetwork.Waypoints[_currentWaypint] != null)
        {
            Transform newWaypoint = _wayPointNetwork.Waypoints[_currentWaypint];

            //This is our new target position
            SetTarget(AITargetType.WAYPOINT, null, newWaypoint.position, Vector3.Distance(newWaypoint.position, transform.position));

            return newWaypoint.position;
        }

        return Vector3.zero;
    }

    private void NextWaypoint()
    {
        if (_randomPatrol && _wayPointNetwork.Waypoints.Count > 1)
        {
            int oldWayPoint = _currentWaypint;
            while (_currentWaypint == oldWayPoint)
            {
                _currentWaypint = Random.Range(0, _wayPointNetwork.Waypoints.Count);
            }
        }
        else
            _currentWaypint = _currentWaypint == _wayPointNetwork.Waypoints.Count - 1 ? 0 : _currentWaypint + 1;
        
    }

    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void SetTarget(AITarget t)
    {
        _target = t;

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = _stoppingDistance;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    //custom stopping distance for special enemies
    public void SetTarget(AITargetType t, Collider c, Vector3 p, float d, float s)
    {
        _target.Set(t, c, p, d);

        if (_targetTrigger != null)
        {
            _targetTrigger.radius = s;
            _targetTrigger.transform.position = _target.position;
            _targetTrigger.enabled = true;
        }
    }

    public void ClearTarget()
    {
        _target.Clear();
        if (_targetTrigger != null)
        {
            _targetTrigger.enabled = false;
        }
    }

    protected virtual void FixedUpdate()
    {
        visualThreat.Clear();
        audioThreat.Clear();

        if (_target.type != AITargetType.NONE)
        {
            _target.distance = Vector3.Distance(_transform.position, _target.position);
        }

        _isTargetReached = false;
    }

    protected virtual void Update()
    {
        if (_currentState == null) return;


        //this is whats pulling out the functions from the sate claas that we are currently assigned
        AIStateType newStateType = _currentState.OnUpdate();
        if(newStateType != _currentStateType)
        {
            AIState newState = null;
            if(_states.TryGetValue(newStateType, out newState))
            {
                //can find correct state
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }
            else if (_states.TryGetValue(AIStateType.IDLE, out newState))
            {
                //can find correct state
                _currentState.OnExitState();
                newState.OnEnterState();
                _currentState = newState;
            }

            _currentStateType = newStateType;
        }
    }

    protected virtual void OnTriggerEnter (Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true;

        if (_currentState)
        {
            _currentState.OnDestinationReached(true);
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (_targetTrigger == null || other != _targetTrigger) return;

        _isTargetReached = true;
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (_targetTrigger == null || _targetTrigger!=other) return;

        _isTargetReached = false;

        if (_currentState!=null)
        {
            _currentState.OnDestinationReached(false);
        }
    }

    public virtual void OnTriggerEvent(AITriggerEventType type, Collider other)
    {
        if(_currentState!= null)
        {
            _currentState.OnTriggerEvent(type, other);
        }
    }

    protected virtual void OnAnimatorMove()
    {
        if (_currentState != null)
            _currentState.OnAnimatorUpdated();
    }

    protected virtual void OnAnimatorIK( int layerIndex )
    {
        if (_currentState != null)
            _currentState.OnAnimatorIKUpdated();
    }

    public void NavAgentControl(bool positionUpdate, bool rotationUpdate)
    {
        if (_navAgent)
        {
            _navAgent.updatePosition = positionUpdate;
            _navAgent.updateRotation = rotationUpdate;
        }
    }

    public void AddRootMotionRequest(int rootPositon, int rootRotation)
    {
        _rootPositionRefCount += rootPositon;
        _rootRotationRefCount += rootRotation;
    }

    public virtual void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart, CharacterManager characterManager, int hitDirection=0)
    {

    }
}
