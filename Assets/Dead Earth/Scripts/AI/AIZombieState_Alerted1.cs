using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Alerted1 : AIZombieState {

    //Inspector Assigned
    [SerializeField] [Range(1.0f,60.0f)] float _maxDuration = 10.0f;
    [SerializeField] float _waypointAngleThreshold = 90.0f;
    [SerializeField] float _threatAngleThreshold = 10.0f;
    [SerializeField] float _directionSwitchTime = 1.5f;
    [SerializeField] float _slerpSpeed = 45.0f;

    //Private Feilds
    private float _timer = 0.0f;
    private float _diretionChangeTimer = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.ALERTED;
    }

    public override void OnEnterState()
    {
        Debug.Log("Enter alerted state");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        //Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0.0f;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        _timer = _maxDuration;
        _diretionChangeTimer = 0.0f;
    }

    public override AIStateType OnUpdate()
    {
        _timer -= Time.deltaTime;
        _diretionChangeTimer += Time.deltaTime;

        //check if timer has run out
        if (_timer <= 0.0f)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navAgent.isStopped = false;
            _timer = _maxDuration;
        }

        if(_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_PLAYER)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            return AIStateType.PURSUIT;
        }

        if (_zombieStateMachine.audioThreat.type == AITargetType.AUDIO)
        {
            //doesnt return aistate allerted because the flashlight is more important so it just resets the time duration
            _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
            _timer = _maxDuration;
        }

        if(_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_LIGHT)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            _timer = _maxDuration;
        }

        if(_zombieStateMachine.audioThreat.type == AITargetType.NONE && _zombieStateMachine.visualThreat.type == AITargetType.VISUAL_FOOD && _zombieStateMachine.targetType == AITargetType.NONE)
        {
            _zombieStateMachine.SetTarget(_stateMachine.visualThreat);
            return AIStateType.PURSUIT;
        }

        //figuring out which way to turn
        float angle;

        if((_zombieStateMachine.targetType==AITargetType.AUDIO || _zombieStateMachine.targetType == AITargetType.VISUAL_LIGHT) && !_zombieStateMachine.isTargetReached)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward, _zombieStateMachine.targetPosition - _zombieStateMachine.transform.position);

            if (_zombieStateMachine.targetType == AITargetType.AUDIO && Mathf.Abs(angle) < _threatAngleThreshold)
            {
                return AIStateType.PURSUIT;
            }
            if (_diretionChangeTimer > _directionSwitchTime)
            {
                //make an informed desition on which way to turn
                if (Random.value < _zombieStateMachine.intelligence)
                {
                    //fed into animator, converted to int
                    _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
                }
                else
                {
                    //move random direction
                    _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                }

                _diretionChangeTimer = 0.0f;
            }
        }
        else
        if(_zombieStateMachine.targetType==AITargetType.WAYPOINT && !_zombieStateMachine.navAgent.pathPending)
        {
            angle = AIState.FindSignedAngle(_zombieStateMachine.transform.forward, _zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position);

            if (Mathf.Abs(angle) < _waypointAngleThreshold) return AIStateType.PATROL;

            if (_diretionChangeTimer > _directionSwitchTime)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(angle);
                _diretionChangeTimer = 0.0f;
            }
        }
        else
        {
            if (_diretionChangeTimer > _directionSwitchTime)
            {
                _zombieStateMachine.seeking = (int)Mathf.Sign(Random.Range(-1.0f, 1.0f));
                _diretionChangeTimer = 0.0f;
            }
        }

        //used for when zombie is crawling
        if (!_zombieStateMachine.useRootRotation) _zombieStateMachine.transform.Rotate(new Vector3(0.0f, _slerpSpeed * _zombieStateMachine.seeking * Time.deltaTime, 0.0f));

        return AIStateType.ALERTED;
    }

}
