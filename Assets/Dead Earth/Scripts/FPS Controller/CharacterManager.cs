using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour {

    //Inspector
    [SerializeField] private CapsuleCollider _meleeTrigger = null;
    [SerializeField] private CameraBloodEffect _cameraBloodEffect = null;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private float _health = 100.0f;
    [SerializeField] private AISoundEmitter _soundEmitter = null;
    [SerializeField] private float _walkRadius = 0.0f;
    [SerializeField] private float _runRadius = 7.0f;
    [SerializeField] private float _landingRadius = 12.0f;
    [SerializeField] private float _bloodRadiusScale = 6.0f;

    //Private
    private Collider _collider = null;
    private FPSController _fpsController = null;
    private CharacterController _characterController = null;
    private GameSceneManager _gameSceneManager = null;
    private int _aiBodyPartLayer = -1;

	// Use this for initialization
	void Start () {
        _collider = GetComponent<Collider>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.instance;

        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");

        if (_gameSceneManager != null)
        {
            PlayerInfo info = new PlayerInfo();
            info.camera = _camera;
            info.characterManager = this;
            info.collider = _collider;
            info.meleeTrigger = _meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(_collider.GetInstanceID(), info);
        }
	}
	
	public void TakeDamage(float amount)
    {
        _health = Mathf.Max(_health - (amount * Time.deltaTime), 0.0f);

        if (_cameraBloodEffect != null)
        {
            _cameraBloodEffect.minBloodAmount = (1.0f - (_health / 100.0f))/3;
            _cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + 0.3f, 10.0f);
        }
    }

    public void DoDamage(int hitDirection = 0)
    {
        if (_camera == null) return;
        if (_gameSceneManager == null) return;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        isSomethingHit = Physics.Raycast(ray, out hit, 1000, 1 << _aiBodyPartLayer);

        if (isSomethingHit)
        {
            //used to search dictionary to find which zombie has been hit with ray
            AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if (stateMachine)
            {
                //parameters taken from current weapon
                stateMachine.TakeDamage(hit.point, ray.direction * 1.0f, 100, hit.rigidbody, this, 0);
            }
        }

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoDamage();
        }

        if (_fpsController && _soundEmitter)
        {
            //health is brought in here for the zombie to smell the play if damaged
            float newRadius = Mathf.Max(_walkRadius, (100.0f - _health) / _bloodRadiusScale);

            switch (_fpsController.movementStatus)
            {
                //only assign if the value is bigger than the current value
                case PlayerMoveStatus.LANDING: newRadius = Mathf.Max(newRadius, _landingRadius); break;
                case PlayerMoveStatus.RUNNING: newRadius = Mathf.Max(newRadius, _runRadius); break;
            }

            _soundEmitter.SetRadius(newRadius);
        }
    }
}
