using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISensor : MonoBehaviour {

    //Private
    private AIStateMachine _parentStateMachine = null;
    public AIStateMachine parentStateMachine { set { _parentStateMachine = value; } }

    private void OnTriggerEnter(Collider col)
    {
        if (_parentStateMachine != null)
             _parentStateMachine.OnTriggerEvent(AITriggerEventType.ENTER, col);        
    }

    private void OnTriggerStay(Collider col)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.STAY, col);
    }

    private void OnTriggerExit(Collider col)
    {
        if (_parentStateMachine != null)
            _parentStateMachine.OnTriggerEvent(AITriggerEventType.EXIT, col);
    }
}
