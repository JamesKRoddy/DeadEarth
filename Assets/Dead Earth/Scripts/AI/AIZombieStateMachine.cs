using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//whats contolling the zombie
public enum AIBoneControlType { ANIMATED, RAGDOLL, RAGDOLLTOANIM }
public enum AIScreamPosition { ENTITY, PLAYER }

public class BodyPartSnapshot
{
    //used when transitioning from a ragdoll back to animator control

    public Transform transform;
    public Vector3 position;
    public Quaternion rotation; 
}

public class AIZombieStateMachine : AIStateMachine {

    // Inspector Assigned
    [SerializeField] [Range(10.0f, 360.0f)] float _fov = 60.0f;
    [SerializeField] [Range(0.0f, 1.0f)] float _sight = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float _hearing = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)] float _aggression = 0.5f;
    [SerializeField] [Range(0, 100)] int _health = 100;
    [SerializeField] [Range(0, 100)] int _lowerBodyDamage = 0;
    [SerializeField] [Range(0, 100)] int _upperBodyDamage = 0;
    [SerializeField] [Range(0, 100)] int _upperBodyThreshold = 30;
    [SerializeField] [Range(0, 100)] int _limpThreshold = 30;
    [SerializeField] [Range(0, 100)] int _crawlThreshold = 90;
    [SerializeField] [Range(0.0f, 1.0f)] float _intelligence = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float _satisfaction = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)] float _screamChance = 1.0f;
    [SerializeField] [Range(0.0f, 50.0f)] float _screamRadius = 20.0f;
    [SerializeField] AIScreamPosition _screamPsition = AIScreamPosition.ENTITY;
    [SerializeField] AISoundEmitter _screamPrefab = null;
    [SerializeField] float _replenishRate = 0.5f;
    [SerializeField] float _depletionRate = 0.5f;
    [SerializeField] float _reanimationBlendTime = 1.5f;
    [SerializeField] float _reanimationWaitTime = 3.0f;
    [SerializeField] LayerMask _geometryLayers = 0;
    

    //Private
    private int _seeking = 0;
    private bool _feeding = false;
    private bool _crawling = false;
    private int _attackType = 0;
    private float _speed = 0.0f;
    private float _isScreaming = 0.0f;

    //Ragdoll Stuff
    private AIBoneControlType _boneControlType = AIBoneControlType.ANIMATED;
    private List<BodyPartSnapshot> _bodyPartSnapShots = new List<BodyPartSnapshot>();
    private float _ragdollEndTime = float.MinValue;
    private Vector3 _ragdollHipPosition;
    private Vector3 _ragdollFeetPosition;
    private Vector3 _ragdollHeadPosition;
    private IEnumerator _reanimationCoroutine = null;
    private float _mecanimTransitionTime = 0.1f;

    //Hashes
    private int _speedHash = Animator.StringToHash("Speed");
    private int _seekingHash = Animator.StringToHash("Seeking");
    private int _feedingHash = Animator.StringToHash("Feeding");
    private int _attackHash = Animator.StringToHash("Attack");
    private int _crawlingHash = Animator.StringToHash("Crawling");
    private int _screamingHash = Animator.StringToHash("Screaming");
    private int _screamHash = Animator.StringToHash("Scream");
    private int _hitTriggerHash = Animator.StringToHash("Hit");
    private int _hitTypeHash = Animator.StringToHash("HitType");
    private int _upperBodyDamageHash = Animator.StringToHash("Upper Body Damage");
    private int _lowerBodyDamageHash = Animator.StringToHash("Lower Body Damage");
    private int _reanimateFromBackHash = Animator.StringToHash("Reanimate From Back");
    private int _reanimateFromFrontHash = Animator.StringToHash("Reanimate From Front");
    private int _stateHash = Animator.StringToHash("State");
    private int _upperBodyLayer = -1;
    private int _lowerBodyLayer = -1;

    // Public Properties
    public float replenishRate { get { return _replenishRate; } }
    public float fov            { get { return _fov; } }
    public float hearing        { get { return _hearing; } }
    public float sight          { get { return _sight; } }
    public bool crawling        { get { return _crawling; } }
    public float intelligence   { get { return _intelligence; } }
    public float satisfaction   { get { return _satisfaction; }     set { _satisfaction = value; } }
    public float aggression     { get { return _aggression; }       set { _aggression = value; } }
    public int health           { get { return _health; }           set { _health = value; } }
    public int attackType       { get { return _attackType; }       set { _attackType = value; } }
    public bool feeding         { get { return _feeding; }          set { _feeding = value; } }
    public int seeking          { get { return _seeking; }          set { _seeking = value; } }
    public float speed
    {
        get { return _speed; }
        set { _speed = value; }
    }
    public bool isCrawling
    {
        get { return (_lowerBodyDamage >= _crawlThreshold); }
    }
    public bool IsScreaming
    {
        get { return _isScreaming > 0.1f; }
    }

    protected override void Update()
    {        
        //overriding the update in the base class i.e. AIStateMachine
        base.Update();

        if (_animator != null)
        {
            _animator.SetFloat(_speedHash, _speed);
            _animator.SetBool(_feedingHash, _feeding);
            _animator.SetInteger(_seekingHash, _seeking);
            _animator.SetInteger(_attackHash, _attackType);
            _animator.SetInteger(_stateHash, (int)_currentStateType);
        }

        _satisfaction = Mathf.Max(0, _satisfaction - ((_depletionRate * Time.deltaTime)/100)* Mathf.Pow(_speed,3));
    }

    protected void UpdateAnimatorDamage()
    {
        if (_animator != null)
        {
            if(_lowerBodyLayer != -1)
            {
                _animator.SetLayerWeight(_lowerBodyLayer, (_lowerBodyDamage > _limpThreshold && _lowerBodyDamage < _crawlThreshold) ? 1.0f : 0.0f);
            }

            if (_upperBodyLayer != -1)
            {
                _animator.SetLayerWeight(_upperBodyLayer, (_upperBodyDamage > _upperBodyThreshold && _lowerBodyDamage < _crawlThreshold) ? 1.0f : 0.0f);
            }
            _animator.SetBool(_crawlingHash, isCrawling);
            _animator.SetInteger(_lowerBodyDamageHash, _lowerBodyDamage);
            _animator.SetInteger(_upperBodyDamageHash, _upperBodyDamage);
        }
    }

    protected override void Start()
    {
        base.Start();

        if (_animator != null)
        {
            //cashe layers
            _lowerBodyLayer = _animator.GetLayerIndex("Lower Body");
            _upperBodyLayer = _animator.GetLayerIndex("Upper Body");
        }

        //get all bones in zombie for reanimation
        if (_rootBone != null)
        {
            Transform[] transforms = _rootBone.GetComponentsInChildren<Transform>();
            foreach (Transform trans in transforms)
            {
                BodyPartSnapshot snapShot = new BodyPartSnapshot();
                snapShot.transform = trans;
                _bodyPartSnapShots.Add(snapShot);
            }
        }

        UpdateAnimatorDamage();
    }


    //function overview
    //Check if we have ragdolled the zombie, if not send info to animator, if ragdolled and still alive then reanimate
    public override void TakeDamage(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart, CharacterManager characterManager, int hitDirection = 0)
    {
        if (GameSceneManager.instance != null && GameSceneManager.instance.bloodParticles != null)
        {
            ParticleSystem sys = GameSceneManager.instance.bloodParticles;
            sys.transform.position = position;
            var settings = sys.main;
            settings.simulationSpace = ParticleSystemSimulationSpace.World;
            sys.Emit(60);

        }

        float hitStrength = force.magnitude;

        //if ragdolled
        if (_boneControlType == AIBoneControlType.RAGDOLL)
        {
            if (bodyPart != null)
            {
                if (hitStrength > 1.0f)
                {
                    bodyPart.AddForce(force, ForceMode.Impulse);
                }

                if (bodyPart.CompareTag("Head"))
                {
                    _health = Mathf.Max(_health - damage, 0);
                }
                else
                if(bodyPart.CompareTag("Upper Body"))
                {
                    _upperBodyDamage += damage;
                }
                else
                if(bodyPart.CompareTag("Lower Body"))
                {
                    _lowerBodyDamage += damage;
                }

                UpdateAnimatorDamage();

                if (_health > 0)
                {
                    if (_reanimationCoroutine!=null)
                        StopCoroutine(_reanimationCoroutine);

                    _reanimationCoroutine = Reanimate();
                    StartCoroutine(_reanimationCoroutine);
                }
            }
            //return because already in a ragdoll state
            return;
        }

        //characters position relative to the zombie
        Vector3 attackerLocalPos = transform.InverseTransformPoint(characterManager.transform.position);

        //local position of the hit point
        Vector3 hitLocPos = transform.InverseTransformPoint(position);

        bool shouldRagdoll = (hitStrength > 1.0f);

        //what happens in normal play if the zombie is hit
        if (bodyPart != null)
        {
            if (bodyPart.CompareTag("Head"))
            {
                _health = Mathf.Max(_health - damage, 0);
                if (health == 0) shouldRagdoll = true;
            }
            else
            if (bodyPart.CompareTag("Upper Body"))
            {
                _upperBodyDamage += damage;
                UpdateAnimatorDamage();
            }
            else
            if (bodyPart.CompareTag("Lower Body"))
            {
                _lowerBodyDamage += damage;
                UpdateAnimatorDamage();
                shouldRagdoll = true;
            }
        }

        //should rag doll: if animated, if crawling, if in cinematic layer, if attack came from behind
        if (_boneControlType != AIBoneControlType.ANIMATED || isCrawling || cinematicEnabled || attackerLocalPos.z < 0) shouldRagdoll = true;

        //should NOT ragdoll: play animation instead
        if (!shouldRagdoll)
        {
            float angle = 0.0f;
            if(hitDirection == 0)
            {
                Vector3 vecToHit = (position - transform.position).normalized;
                angle = AIState.FindSignedAngle(vecToHit, transform.forward);
            }

            //decide animation to play, hit direction is used for melee weapons
            int hitType = 0;
            if (bodyPart.gameObject.CompareTag("Head"))
            {
                if (angle < -10 || hitDirection == -1)      hitType = 1;
                else
                if (angle > 10 || hitDirection == 1)        hitType = 3;
                else                                        hitType = 2;
            }
            else
            if(bodyPart.gameObject.CompareTag("Upper Body"))
            {
                if (angle < -20 || hitDirection == -1)      hitType = 4;
                else
                if (angle > 20 || hitDirection == 1)        hitType = 6;
                else                                        hitType = 5;
            }
            //put in more hit animation parameters here
            //TODO try with rb reacting to forces

            if (_animator)
            {
                _animator.SetInteger(_hitTypeHash, hitType);
                _animator.SetTrigger(_hitTriggerHash);
            }

            return;
        }
        else
        {
            //stops the machine from calling a state, shuts off AIStateMachine
            if (_currentState)
            {
                _currentState.OnExitState();
                _currentState = null;
                _currentStateType = AIStateType.NONE;
            }

            if (_navAgent) _navAgent.enabled = false;
            if (_animator) _animator.enabled = false;
            if (_collider) _collider.enabled = false;

            inMeleeRange = false;

            

            foreach(Rigidbody body in _bodyParts)
            {
                if (body)
                {
                    //ragdoll body
                    body.isKinematic = false;
                }
            }

            if (hitStrength > 1.0f)
            {
                bodyPart.AddForce(force, ForceMode.Impulse);
            }

            _boneControlType = AIBoneControlType.RAGDOLL;

            if(_health > 0)
            {
                if (_reanimationCoroutine!=null)
                    StopCoroutine(_reanimationCoroutine);

                _reanimationCoroutine = Reanimate();
                StartCoroutine(_reanimationCoroutine);
            }
        }
    }


    protected IEnumerator Reanimate()
    {
        //Only reanimate in ragdoll state
        if (_boneControlType != AIBoneControlType.RAGDOLL || _animator == null) yield break;

        //wait for desired time
        yield return new WaitForSeconds(_reanimationWaitTime);

        //record time at start of reanimation
        _ragdollEndTime = Time.time;

        //set rbs in body to kinematic so animator can take hold
        foreach (Rigidbody body in _bodyParts)
        {
            body.isKinematic = true;
        }

        //put into reanimation mode
        _boneControlType = AIBoneControlType.RAGDOLLTOANIM;

        //record pos and rot of all bones before reanimation
        foreach (BodyPartSnapshot snapShot in _bodyPartSnapShots)
        {
            snapShot.position = snapShot.transform.position;
            snapShot.rotation = snapShot.transform.rotation;
        }

        //record ragdoll head and feet position
        _ragdollHeadPosition = _animator.GetBoneTransform(HumanBodyBones.Head).position;
        _ragdollFeetPosition = (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + _animator.GetBoneTransform(HumanBodyBones.RightFoot).position) * 0.5f;
        _ragdollHipPosition = _rootBone.position;

        //enable animator
        _animator.enabled = true;       

        if(_rootBone!=null)
        {
            //figure out which animation to play based on orientation
            float forwardTest;

            switch (_rootBoneAlignment)
            {
                //forward axis of model(used on models that are orientated wrong when imported)
                case AIBoneAlignmentType.ZAXIS:
                    forwardTest = _rootBone.forward.y; break;
                case AIBoneAlignmentType.ZAXISINVERTED:
                    forwardTest = -_rootBone.forward.y; break;
                case AIBoneAlignmentType.YAXIS:
                    forwardTest = _rootBone.up.y; break;
                case AIBoneAlignmentType.YAXISINVERTED:
                    forwardTest = -_rootBone.up.y; break;
                case AIBoneAlignmentType.XAXIS:
                    forwardTest = _rootBone.right.y; break;
                case AIBoneAlignmentType.XAXISINVERTED:
                    forwardTest = -_rootBone.right.y; break;
                default:
                    forwardTest = _rootBone.forward.y; break;
            }

            if (forwardTest >= 0)
                _animator.SetTrigger(_reanimateFromBackHash);
            else
                _animator.SetTrigger(_reanimateFromFrontHash);
        }
    }

    //called at the end of every frame
    protected virtual void LateUpdate()
    {
        if(_boneControlType == AIBoneControlType.RAGDOLLTOANIM)
        {
            //Aligning the parent obj to the zombie
            if(Time.time <= _ragdollEndTime + _mecanimTransitionTime)
            {
                Vector3 animatedToRagdoll = _ragdollHipPosition - _rootBone.position;
                Vector3 newRootPosition = transform.position + animatedToRagdoll;

                //Vector3.up makes sure the ray isnt cast from under the floor
                RaycastHit[] hits = Physics.RaycastAll(newRootPosition + (Vector3.up * 0.25f), Vector3.down, float.MaxValue, _geometryLayers);
                newRootPosition.y = float.MinValue;
                foreach(RaycastHit hit in hits)
                {
                    //make sure the object is not attached to the zombie
                    if (!hit.transform.IsChildOf(transform))
                    {
                        //if the current y is larger that the current one we have set assign it
                        newRootPosition.y = Mathf.Max(hit.point.y, newRootPosition.y);
                    }
                }

                //make sure point is on the nav mesh, use this to pass in a position and find the closest point on the nav mesh
                NavMeshHit navMeshHit;
                Vector3 baseOffset = Vector3.zero;
                if (_navAgent) baseOffset.y = _navAgent.baseOffset;
                if(NavMesh.SamplePosition(newRootPosition, out navMeshHit, 2.0f, NavMesh.AllAreas))
                {
                    //if it can find an area on the nav mesh
                    transform.position = navMeshHit.position + baseOffset;
                }
                else
                {
                    //else set this as our new transform
                    transform.position = newRootPosition + baseOffset;
                }

                Vector3 ragdollDirection = _ragdollHeadPosition - _ragdollFeetPosition;
                //set y to 0
                ragdollDirection.y = 0.0f;

                //mid point between feet
                Vector3 meanFeetPosition = 0.5f*(_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + _animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                Vector3 animatedDirection = _animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
                //set y to 0
                animatedDirection.y = 0;

                //try to match rotation, rotating only around Y as animated character mys stay upright
                //this is y components are set to 0 above
                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdollDirection.normalized);
            }

            //calcutae interpolation t value
            float blendAmount = Mathf.Clamp01((Time.time - _ragdollEndTime - _mecanimTransitionTime) / _reanimationBlendTime);

            //calculate blended bone transforms by interplating between ragdoll bone snapshots and animator positions
            foreach(BodyPartSnapshot snapshot in _bodyPartSnapShots)
            {
                //hip bone from ragdoll and animated version my have transformed so it has to be moved, all other bones are childed so only rotation need to be changed
                if(snapshot.transform == _rootBone)
                {
                    snapshot.transform.position = Vector3.Lerp(snapshot.position, snapshot.transform.position, blendAmount);
                }

                snapshot.transform.rotation = Quaternion.Slerp(snapshot.rotation, snapshot.transform.rotation, blendAmount);
            }

            //hand control back to animator
            if(blendAmount == 1.0f)
            {
                _boneControlType = AIBoneControlType.ANIMATED;
                if (_navAgent) _navAgent.enabled = true;
                if (_collider) _collider.enabled = true;

                AIState newState = null;
                if(_states.TryGetValue(AIStateType.ALERTED,out newState))
                {
                    if (_currentState != null) _currentState.OnExitState();
                    newState.OnEnterState();
                    _currentState = newState;
                    _currentStateType = AIStateType.ALERTED;
                }
            }
        }
    }
}
