using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Feeding1 : AIZombieState {

    //Insector Assigned
    [SerializeField] float _slerpSpeed = 5.0f;
    [SerializeField] Transform _bloodParticlesMount = null;
    [SerializeField] [Range(0.01f, 1.0f)] float _bloddParticleBurstTime = 0.1f;
    [SerializeField] [Range(1, 100)] int _bloodParticlesBurstAmmount = 10;

    //Private 
    private int _eatingStateHash = Animator.StringToHash("Feeding State");
    private int _crawlEatingStateHash = Animator.StringToHash("Crawl Feeding");
    private int _eatingLayerIndex = -1;
    private float _timer = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.FEEDING;
    }

    public override void OnEnterState()
    {
        Debug.Log("entered feeding state");

        base.OnEnterState();
        if (_zombieStateMachine == null) return;

        //Get layer index
        if (_eatingLayerIndex == -1)
            _eatingLayerIndex = _zombieStateMachine.animator.GetLayerIndex("Cinematic");

        _timer = 0.0f;

        //Configure State Machine
        _zombieStateMachine.feeding = true;
        _zombieStateMachine.speed = 0;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.attackType = 0;
        _zombieStateMachine.NavAgentControl(true, false);
    }

    public override void OnExitState()
    {
        if (_zombieStateMachine != null)
            _zombieStateMachine.feeding = false;
    }

    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime;

        if(_zombieStateMachine.satisfaction > 0.9f)
        {
            _zombieStateMachine.GetWaypointPosition(false);
            return AIStateType.ALERTED;
        }

        //might put in an extra parameter to say if satisfaction is really low ignore the player
        if(_zombieStateMachine.visualThreat.type != AITargetType.NONE && _zombieStateMachine.visualThreat.type != AITargetType.VISUAL_FOOD)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            return AIStateType.ALERTED;
        }

        if(_zombieStateMachine.audioThreat.type == AITargetType.AUDIO)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
            return AIStateType.ALERTED;
        }

        //if our state on the cinematic layer is either eating state
        int currentHash = _zombieStateMachine.animator.GetCurrentAnimatorStateInfo(_eatingLayerIndex).shortNameHash;
        if (currentHash == _eatingStateHash || currentHash == _crawlEatingStateHash)
        {
            _zombieStateMachine.satisfaction = Mathf.Min(_zombieStateMachine.satisfaction + (Time.deltaTime * _zombieStateMachine.replenishRate)/100.0f, 1.0f);
            //Particle system stuff
            if(GameSceneManager.instance && GameSceneManager.instance.bloodParticles && _bloodParticlesMount)
            {
                if(_timer > _bloddParticleBurstTime)
                {
                    ParticleSystem system = GameSceneManager.instance.bloodParticles;
                    system.transform.position = _bloodParticlesMount.transform.position;
                    system.transform.rotation = _bloodParticlesMount.transform.rotation;
                    var settings = system.main;
                    settings.simulationSpace = ParticleSystemSimulationSpace.World;
                    system.Emit(_bloodParticlesBurstAmmount);
                    _timer = 0.0f;
                }
            }
        }

        if (!_zombieStateMachine.useRootRotation)
        {
            //keeps zombie facing food
            Vector3 targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);

        }

        //moves zombie towqards target
        Vector3 headToTarget = _zombieStateMachine.targetPosition - _zombieStateMachine.animator.GetBoneTransform(HumanBodyBones.Head).position;
        _zombieStateMachine.transform.position = Vector3.Lerp(_zombieStateMachine.transform.position, _zombieStateMachine.transform.position + headToTarget, Time.deltaTime);

        //Default
        return AIStateType.FEEDING;
    }

}
