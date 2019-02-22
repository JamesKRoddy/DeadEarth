using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Attack1 : AIZombieState {

    //Public assigned
    [SerializeField] [Range(0, 10)] float _speed = 0.0f;
    [SerializeField] [Range(0, 1)] float _lookAtWeight = 0.7f;
    [SerializeField] [Range(0, 90)] float _lookAtAngleThreshold = 15.0f;
    [SerializeField] float _slerpSpeed = 5.0f;
    [SerializeField] float _stoppingDistance = 0.0f;

    //Private
    private float _currentLookAtWeight = 0.0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.ATTACK;
    }

    public override void OnEnterState()
    {
        Debug.Log("Entered Attack state");

        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        //Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = _speed;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = Random.Range(1, 100);
        _currentLookAtWeight = 0.0f;

    }

    public override AIStateType OnUpdate()
    {
        Vector3 targetPos;
        Quaternion newRot;

        if(Vector3.Distance(_zombieStateMachine.transform.position, _zombieStateMachine.targetPosition) < _stoppingDistance)
        {
            _zombieStateMachine.speed = 0;
        }
        else
        {
            _zombieStateMachine.speed = _speed;
        }

        if(_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_PLAYER)
        {
            _zombieStateMachine.SetTarget(_stateMachine.visualThreat);

            if (!_zombieStateMachine.inMeleeRange) return AIStateType.PURSUIT;

            if (!_zombieStateMachine.useRootRotation)
            {
                //Keep zombie facing player
                targetPos = _zombieStateMachine.targetPosition;
                targetPos.y = _zombieStateMachine.transform.position.y;
                newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
                _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
            }

            _zombieStateMachine.attackType = Random.Range(1, 100);

            return AIStateType.ATTACK;
        }

        if (!_zombieStateMachine.useRootRotation)
        {
            targetPos = _zombieStateMachine.targetPosition;
            targetPos.y = _zombieStateMachine.transform.position.y;
            newRot = Quaternion.LookRotation(targetPos - _zombieStateMachine.transform.position);
            _zombieStateMachine.transform.rotation = newRot;
        }

        //Default
        return AIStateType.ALERTED;
    }

    public override void OnAnimatorIKUpdated()
    {
        if (_zombieStateMachine == null)
            return;

        if(Vector3.Angle(_zombieStateMachine.transform.forward,_zombieStateMachine.targetPosition - _zombieStateMachine.transform.position) < _lookAtAngleThreshold)
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
