using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDamageTrigger : MonoBehaviour {

    [SerializeField] string _parameter = "";
    [SerializeField] int _bloodParticlesBurstAmmount = 10;
    [SerializeField] float _damageAmount =0.1f;

    //Private
    AIStateMachine _stateMachine = null;
    Animator _animator = null;
    int _parameterHash = -1;
    GameSceneManager _gameSceneManager = null;

    private void Start()
    {
        //root gets to rootobject
        _stateMachine = transform.root.GetComponentInChildren<AIStateMachine>();
        if(_stateMachine != null)
        {
            _animator = _stateMachine.animator;
        }

        _parameterHash = Animator.StringToHash(_parameter);

        _gameSceneManager = GameSceneManager.instance;
    }

    private void OnTriggerStay(Collider col)
    {
        if (!_animator) return;
        if (col.gameObject.CompareTag("Player") && _animator.GetFloat(_parameterHash) > 0.9f)
        {
            if(GameSceneManager.instance && GameSceneManager.instance.bloodParticles)
            {
                ParticleSystem system = GameSceneManager.instance.bloodParticles;

                //Temp Code
                system.transform.position = transform.position;
                system.transform.rotation = Camera.main.transform.rotation;

                var settings = system.main;
                settings.simulationSpace = ParticleSystemSimulationSpace.World;
                system.Emit(_bloodParticlesBurstAmmount);
            }
            
            if(_gameSceneManager != null)
            {
                PlayerInfo info = _gameSceneManager.GetPlayerInfo(col.GetInstanceID());
                if (info != null && info.characterManager!=null)
                {
                    info.characterManager.TakeDamage(_damageAmount);
                }
            }
        }
    }
}
