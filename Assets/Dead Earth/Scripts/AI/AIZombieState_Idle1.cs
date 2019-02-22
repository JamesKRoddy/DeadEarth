using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIZombieState_Idle1 : AIZombieState {

    //Inspector Asssigned
    [SerializeField] Vector2 _idleTimeRange = new Vector2(10.0f, 60.0f);

    //private
    float _idleTime = 0.0f;
    float _timer = 0.0f;

    public override AIStateType GetStateType()
    {        
        return AIStateType.IDLE;
    }

    public override void OnEnterState()
    {
        Debug.Log("Enter idle state");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        //set idle time
        _idleTime = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
        _timer = 0.0f;

        //Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.speed = 0.0f;
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;
        _zombieStateMachine.ClearTarget();
    }

    public override AIStateType OnUpdate()
    {
        if (_zombieStateMachine == null)
            return AIStateType.IDLE;

        if (_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_PLAYER)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            return AIStateType.PURSUIT;
        }

        if (_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_LIGHT)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            return AIStateType.ALERTED;
        }

        if (_zombieStateMachine.audioThreat.type == AITargetType.AUDIO)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.audioThreat);
            return AIStateType.ALERTED;
        }

        if (_zombieStateMachine.visualThreat.type == AITargetType.VISUAL_FOOD)
        {
            _zombieStateMachine.SetTarget(_zombieStateMachine.visualThreat);
            return AIStateType.PURSUIT;
        }

        _timer += Time.deltaTime;

        if (_timer > _idleTime)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.navAgent.isStopped = false;
            return AIStateType.ALERTED;
        }

       return AIStateType.IDLE;
    }

}
