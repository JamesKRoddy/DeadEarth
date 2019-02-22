using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionConfigurator : AIStateMachineLink {

    [SerializeField] private int _rootPostion = 0;
    [SerializeField] private int _rootRotation = 0;

    //used to make sure the enter state has been processed before moving into the exit state
    private bool _rootMotionProcessed = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine)
        {
            Debug.Log(_stateMachine.GetType().ToString());
            _stateMachine.AddRootMotionRequest(_rootPostion, _rootRotation);
            _rootMotionProcessed = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_stateMachine && _rootMotionProcessed) {
            _stateMachine.AddRootMotionRequest(-_rootPostion, -_rootRotation);
            _rootMotionProcessed = false;
        }
    }

}
