using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIZombieState : AIState
{

    //Private
    protected int _playerLayerMask = -1;
    protected int _bodyPartLayer = -1;
    protected int _visualLayerMask = -1;
    protected AIZombieStateMachine _zombieStateMachine = null;

    protected void Awake()
    {
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part") + 1; // +1 correspondes to default layer (try string later)
        _playerLayerMask = LayerMask.GetMask("Player", "AI Body Part", "Visual Aggravator") + 1;
        _bodyPartLayer = LayerMask.NameToLayer("AI Body Part");
    }


    public override void SetStateMachine(AIStateMachine stateMachine)
    {
        if (stateMachine.GetType() == typeof(AIZombieStateMachine))
        {
            base.SetStateMachine(stateMachine);
            _zombieStateMachine = (AIZombieStateMachine)stateMachine;
        }
    }

    public override void OnTriggerEvent(AITriggerEventType eventType, Collider other)
    {
        if (_zombieStateMachine == null) return;

        if (eventType != AITriggerEventType.EXIT)
        {
            AITargetType curType = _stateMachine.visualThreat.type;

            if (other.CompareTag("Player"))
            {
                float distance = Vector3.Distance(_zombieStateMachine.sensorPosition, other.transform.position);
                if (curType != AITargetType.VISUAL_PLAYER || (curType == AITargetType.VISUAL_PLAYER && distance < _zombieStateMachine.visualThreat.distance))
                {
                    RaycastHit hitInfo;

                    if (ColliderIsVisable(other, out hitInfo, _playerLayerMask))
                    {
                        //its close and in our FOV
                        _zombieStateMachine.visualThreat.Set(AITargetType.VISUAL_PLAYER, other, other.transform.position, distance);
                    }
                }
            }
            else if (other.CompareTag("Flash Light") && curType != AITargetType.VISUAL_PLAYER)
            {
                BoxCollider flashLightTrigger = (BoxCollider)other;
                float distanceToThreat = Vector3.Distance(_zombieStateMachine.sensorPosition, flashLightTrigger.transform.position);
                float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
                float aggrFactor = distanceToThreat / zSize;
                if (aggrFactor <= _zombieStateMachine.sight && aggrFactor <= _zombieStateMachine.intelligence)
                {
                    _zombieStateMachine.visualThreat.Set(AITargetType.VISUAL_LIGHT, other, other.transform.position, distanceToThreat);
                }
            }
            else
            if (other.CompareTag("AI Sound Emitter"))
            {
                SphereCollider soundTrigger = (SphereCollider)other;
                if (soundTrigger == null) return;

                //Get the position of the Agent Sensor
                Vector3 agentSensorPosition = _zombieStateMachine.sensorPosition;

                Vector3 soundPos;
                float soundRadius;
                AIState.ConvertSphereColliderToWorldSpace(soundTrigger, out soundPos, out soundRadius);

                //How far inside the sounds radius are we
                float distanceToThreat = (soundPos - agentSensorPosition).magnitude;

                //calculates distance factor 1 when at edge 0 at centre
                float distanceFactor = (distanceToThreat / soundRadius);

                //bias the factor based on hearing ability of agent
                distanceFactor += distanceFactor * (1.0f - _zombieStateMachine.hearing);

                //Too Far away
                if (distanceFactor > 1.0f) return;

                //TODO issue is here
                //if we can hear it and it is closer than what we previously have stored
                if (distanceToThreat < _zombieStateMachine.audioThreat.distance)
                {
                    //Most dangerous threat so far
                    _zombieStateMachine.audioThreat.Set(AITargetType.AUDIO, other, soundPos, distanceToThreat);

                }
            }
            else
            if (other.CompareTag("AI Food") && curType != AITargetType.VISUAL_PLAYER && curType != AITargetType.VISUAL_LIGHT && _zombieStateMachine.satisfaction <= 0.9f && _zombieStateMachine.audioThreat.type == AITargetType.NONE)
            {
                float distanceToThreat = Vector3.Distance(other.transform.position, _zombieStateMachine.sensorPosition);

                if (distanceToThreat < _zombieStateMachine.visualThreat.distance)
                {
                    RaycastHit hitInfo;
                    if (ColliderIsVisable(other, out hitInfo, _visualLayerMask))
                    {
                        _zombieStateMachine.visualThreat.Set(AITargetType.VISUAL_FOOD, other, other.transform.position, distanceToThreat);
                    }
                }
            }

        }
    }

    protected virtual bool ColliderIsVisable(Collider other, out RaycastHit hitInfo, int layerMask = -1)
    {
        hitInfo = new RaycastHit();

        if (_zombieStateMachine == null) return false;

        Vector3 head = _stateMachine.sensorPosition;
        Vector3 direction = other.transform.position - head;
        float angle = Vector3.Angle(direction, transform.forward);

        if (angle > _zombieStateMachine.fov * 0.5f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(head, direction.normalized, _zombieStateMachine.sensorRadius * _zombieStateMachine.sight, layerMask);

        float closestColliderDistance = float.MaxValue;
        Collider closestCollider = null;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.distance < closestColliderDistance)
            {

                if (hit.transform.gameObject.layer == _bodyPartLayer)
                {
                    //make sure it doesnt hit itself, if its hit a gameobject with an AI state machine check if its ours
                    if (_stateMachine != GameSceneManager.instance.GetAIStateMachine(hit.rigidbody.GetInstanceID()))
                    {
                        closestColliderDistance = hit.distance;
                        closestCollider = hit.collider;
                        hitInfo = hit;
                    }
                }
                else
                {
                    closestColliderDistance = hit.distance;
                    closestCollider = hit.collider;
                    hitInfo = hit;
                }
            }
        }

        if (closestCollider && closestCollider.gameObject == other.gameObject) return true;

        return false;
    }
}



