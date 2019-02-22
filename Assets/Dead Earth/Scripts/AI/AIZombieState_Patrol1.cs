using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIZombieState_Patrol1 : AIZombieState {

    //Inspector Assigned
    [SerializeField] float _turnOnSpotThreshold = 80.0f;
    [SerializeField] float _slerpSpeed = 5.0f;

    [SerializeField] [Range(0.0f, 3.0f)] float _speed = 1.0f;


    public override AIStateType GetStateType()
    {
        return AIStateType.PATROL;
    }

    public override void OnEnterState()
    {
        Debug.Log("Enter patrol state");
        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        //Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;

        // Set Destination
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        _zombieStateMachine.navAgent.isStopped = false;
    }

    public override AIStateType OnUpdate()
    {
        //can see player
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
            //If the distance to hunger ratio means that we are hungry and close enough to stray from our path
            if((1.0f-_zombieStateMachine.satisfaction) > (_zombieStateMachine.visualThreat.distance / _zombieStateMachine.sensorRadius))
            {
                _stateMachine.SetTarget(_stateMachine.visualThreat);
                return AIStateType.PURSUIT;
            }
        }

        //path pending, makes zombie stay stil;l if path is being calculated
        if (_zombieStateMachine.navAgent.pathPending)
        {
            _zombieStateMachine.speed = 0;
            return AIStateType.PATROL;
        }
        else
        {
            _zombieStateMachine.speed = _speed;
        }


        //turning on spot
        float angle = Vector3.Angle(_zombieStateMachine.transform.forward, (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));

        if (angle > _turnOnSpotThreshold)
        {
            return AIStateType.ALERTED;
        }


        //if we are not using root rotation, using the nav agents rotation
        if (!_zombieStateMachine.useRootRotation)
        {
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot, Time.deltaTime * _slerpSpeed);
        }

        //checks that waypoint is invalid or stale

        if(_zombieStateMachine.navAgent.isPathStale || !_zombieStateMachine.navAgent.hasPath || _zombieStateMachine.navAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }

        //Stay in patrol state
        return AIStateType.PATROL;

    }   

    public override void OnDestinationReached(bool isReached)
    {
        if(_zombieStateMachine==null || !isReached)
        {
            return;
        }

        if (_zombieStateMachine.targetType == AITargetType.WAYPOINT)
        {
            _zombieStateMachine.GetWaypointPosition(true);
        }
    }

    //public override void OnAnimatorIKUpdated()
    //{
    //    if (_zombieStateMachine == null)
    //    {
    //        return;
    //    }

    //    //head looking at target
    //    _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.targetPosition + Vector3.up); //vector 3 up stops it from looking at the ground
    //    _zombieStateMachine.animator.SetLookAtWeight(0.55f);
    //}
}
