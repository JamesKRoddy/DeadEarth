using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIZombieState_Pursuit1 : AIZombieState {

    [SerializeField] [Range(0, 10)] private float _speed = 1.0f;
    [SerializeField] [Range(0, 1)] float _lookAtWeight = 0.7f;
    [SerializeField] [Range(0, 90)] float _lookAtAngleThreshold = 15.0f;
    [SerializeField] private float _slerpSpeed = 5.0f;
    [SerializeField] private float _repathDistanceMultiplier = 0.035f; //used to stop path to the target being recalculated everyfrme but increse calculations with distance
    [SerializeField] private float _repathVisualMinDuration = 0.05f;
    [SerializeField] private float _repathVisualMaxDuration = 5.0f;
    [SerializeField] private float _repathAudioMinDuration = 0.25f;
    [SerializeField] private float _repathAudioMaxDuration = 5.0f;
    [SerializeField] private float _maxDuration = 40.0f;
    

    //Private Fields
    private float _timer = 0.0f;
    private float _repathTimer = 0.0f;
    private float _currentLookAtWeight = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.PURSUIT;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered pursuit state");

        base.OnEnterState();
        if (_stateMachine == null) return;

        //Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;
        _currentLookAtWeight = 0.0f;

        //Zombie will only pursue for so long
        _timer = 0.0f; //how long in state
        _repathTimer = 0.0f;

        //Set Path
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.targetPosition);
        _zombieStateMachine.navAgent.Resume();
    }


    public override AIStateType OnUpdate()
    {
        _timer += Time.deltaTime;
        _repathTimer += Time.deltaTime;
        if (_timer > _maxDuration)
            return AIStateType.PATROL;

        if(_stateMachine.targetType == AITargetType.VISUAL_PLAYER && _zombieStateMachine.inMeleeRange)
        {
            return AIStateType.ATTACK;
        }

        if (_zombieStateMachine.isTargetReached)
        {
            switch (_stateMachine.targetType)
            {
                //If we reached the source
                case AITargetType.AUDIO:
                case AITargetType.VISUAL_LIGHT:
                    _stateMachine.ClearTarget();
                    return AIStateType.ALERTED;

                case AITargetType.VISUAL_FOOD:
                    return AIStateType.FEEDING;
            }
        }

        //Lost target
        if(_zombieStateMachine.navAgent.isPathStale || (!_zombieStateMachine.navAgent.hasPath && !_zombieStateMachine.navAgent.pathPending) || _zombieStateMachine.navAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            return AIStateType.ALERTED;
        }

        //path pending
        if (_zombieStateMachine.navAgent.pathPending)
            _zombieStateMachine.speed = 0;
        else
        {
            _zombieStateMachine.speed = _speed;

            //Target Reached
            //if close keep facing the player
            if (!_zombieStateMachine.useRootRotation && _zombieStateMachine.targetType == AITargetType.VISUAL_PLAYER && _zombieStateMachine.visualThreat.type == AITargetType.VISUAL_PLAYER && _zombieStateMachine.isTargetReached)
            {
                Vector3 targetPos = _zombieStateMachine.targetPosition;
                targetPos.y = _zombieStateMachine.transform.position.y;
                Quaternion newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
                _zombieStateMachine.transform.rotation = newRot;
            }
            else
            //slowly update zombie rotation when further from player
            if (!_zombieStateMachine.useRootRotation && !_zombieStateMachine.isTargetReached)
            {
                Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);

                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
            }
            else
            //reached audio source of not the player
            if (_zombieStateMachine.isTargetReached)
            {
                return AIStateType.ALERTED;
            }
        }



        //curret threat is player
        if(_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_PLAYER)
        {
            //The path has changed player has moved
            if (_zombieStateMachine.targetPosition != _zombieStateMachine.visualThreat.position)
            {
                if (Mathf.Clamp(_zombieStateMachine.visualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                {
                    _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.visualThreat.position);
                    _repathTimer = 0.0f;
                }
            }

            //make sure this is the current target
            _stateMachine.SetTarget(_zombieStateMachine.visualThreat);

            return AIStateType.PURSUIT;
        }

        //Last known position of the player
        if(_zombieStateMachine.targetType == AITargetType.VISUAL_PLAYER)
        {
            return AIStateType.PURSUIT;
        }

        if(_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_LIGHT)
        {
            if(_zombieStateMachine.targetType == AITargetType.AUDIO || _zombieStateMachine.targetType == AITargetType.VISUAL_FOOD)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
                return AIStateType.ALERTED;
            }
            else
            if(_zombieStateMachine.targetType == AITargetType.VISUAL_LIGHT)
            {
                int currentID = _zombieStateMachine.targetColliderID;

                //Same light
                if(currentID == _zombieStateMachine.visualThreat.collider.GetInstanceID())
                {
                    //light has moved
                    if (_zombieStateMachine.targetPosition != _zombieStateMachine.visualThreat.position)
                    {
                        if (Mathf.Clamp(_zombieStateMachine.visualThreat.distance * _repathDistanceMultiplier, _repathVisualMinDuration, _repathVisualMaxDuration) < _repathTimer)
                        {
                            //Repath
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.visualThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
                    return AIStateType.PURSUIT;
                }
                else
                {
                    //new light source
                    _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
                    return AIStateType.ALERTED;
                }
            }
        }
        else
        if(_zombieStateMachine.audioThreat.type == AITargetType.AUDIO)
        {
            if(_zombieStateMachine.targetType == AITargetType.VISUAL_FOOD)
            {
                _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
                return AIStateType.ALERTED;
            }
            else
            if(_zombieStateMachine.targetType == AITargetType.AUDIO)
            {
                int currentID = _zombieStateMachine.targetColliderID;
                //same audio source
                if(currentID == _zombieStateMachine.audioThreat.collider.GetInstanceID())
                {
                    if(_zombieStateMachine.targetPosition != _zombieStateMachine.audioThreat.position)
                    {
                        if (Mathf.Clamp(_zombieStateMachine.audioThreat.distance * _repathDistanceMultiplier, _repathAudioMinDuration, _repathAudioMaxDuration) < _repathTimer)
                        {
                            //Repath agent
                            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.audioThreat.position);
                            _repathTimer = 0.0f;
                        }
                    }

                    _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
                    return AIStateType.PURSUIT;
                }
                else
                {
                    _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
                    return AIStateType.ALERTED;
                }
            }
        }


        //Default
        return AIStateType.PURSUIT;
    }

    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
            return;

        if (Vector3.Angle(_zombieStateMachine.transform.forward, _zombieStateMachine.targetPosition - _zombieStateMachine.transform.position) < _lookAtAngleThreshold)
        {
            _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up);
            _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, _lookAtWeight, Time.deltaTime);
            _zombieStateMachine.animator.SetLookAtWeight(_currentLookAtWeight);
        }
        else
        {
            _currentLookAtWeight = Mathf.Lerp(_currentLookAtWeight, 0.0f, Time.deltaTime);
            _zombieStateMachine.animator.SetLookAtWeight(_currentLookAtWeight);
        }
    }

}
