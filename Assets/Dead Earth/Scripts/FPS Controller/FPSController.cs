using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Enumerations
public enum PlayerMoveStatus { NOTMOVING, WALKING, RUNNING, NOTGROUNDED, LANDING, CROUCHING }
public enum CurveControlledBobCallbackType { HORIZONTAL, VERTICAL}

//Delegates
public delegate void CurveControlledBobCallback();

[System.Serializable]
public class CurveControlledBobEvent
{
    public float Time = 0.0f;
    public CurveControlledBobCallback Function = null;
    public CurveControlledBobCallbackType Type = CurveControlledBobCallbackType.VERTICAL;
       
}

[System.Serializable]
public class CurveControlledBob
{
    [SerializeField] AnimationCurve _bobCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f), new Keyframe(1.5f, -1f), new Keyframe(2f, 0f));

    [SerializeField] float _horizontalMultiplier = 0.01f;
    [SerializeField] float _verticalMultiplier = 0.02f;
    [SerializeField] float _verticalToHorizontalSpeedRatio = 2.0f;
    [SerializeField] float _baseInterval;

    //Internals
    private float _prevXPlayHead;
    private float _prevYPlayHead;
    private float _xPlayHead;
    private float _yPlayHead;    
    private float _curveEndTime;
    private List<CurveControlledBobEvent> _events = new List<CurveControlledBobEvent>();

    public void Initialize()
    {
        _curveEndTime = _bobCurve[_bobCurve.length - 1].time;
        _xPlayHead = 0.0f;
        _yPlayHead = 0.0f;
        _prevXPlayHead = 0.0f;
        _prevYPlayHead = 0.0f;
    }

    public void RegisterEventCallback (float time, CurveControlledBobCallback function, CurveControlledBobCallbackType type)
    {
        CurveControlledBobEvent ccbeEvent = new CurveControlledBobEvent();
        ccbeEvent.Time = time;
        ccbeEvent.Function = function;
        ccbeEvent.Type = type;
        _events.Add(ccbeEvent);
        //which should be put infront of eachother in the list
        _events.Sort(
            delegate (CurveControlledBobEvent t1, CurveControlledBobEvent t2)
            {
                return (t1.Time.CompareTo(t2.Time));
            }
        );
    }

    public Vector3 GetVectorOffset(float speed)
    {
        _xPlayHead += (speed * Time.deltaTime) / _baseInterval;
        _yPlayHead += ((speed * Time.deltaTime) / _baseInterval) * _verticalToHorizontalSpeedRatio;

        //wrap it back aound to the start of the curve
        if (_xPlayHead > _curveEndTime)
            _xPlayHead -= _curveEndTime;

        if (_yPlayHead > _curveEndTime)
            _yPlayHead -= _curveEndTime;

        //Process Events
        for (int i = 0; i < _events.Count; i++)
        {
            CurveControlledBobEvent ev = _events[i];
            if (ev != null)
            {
                if(ev.Type == CurveControlledBobCallbackType.VERTICAL)
                {
                    if((_prevYPlayHead < ev.Time && _yPlayHead >=ev.Time) || (_prevYPlayHead > _yPlayHead && (ev.Time > _prevYPlayHead || ev.Time <= _yPlayHead)))
                    {
                        ev.Function();
                    }
                }
                else
                {
                    if ((_prevXPlayHead < ev.Time && _xPlayHead >= ev.Time) || (_prevXPlayHead > _xPlayHead && (ev.Time > _prevXPlayHead || ev.Time <= _xPlayHead)))
                    {
                        ev.Function();
                    }
                }
            }
        }

        float xPos = _bobCurve.Evaluate(_xPlayHead) * _horizontalMultiplier;
        float yPos = _bobCurve.Evaluate(_yPlayHead) * _verticalMultiplier;

        _prevXPlayHead = _xPlayHead;
        _prevYPlayHead = _yPlayHead;

        return new Vector3(xPos, yPos, 0f);
    }


}

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour {

    //Temp Code
    public List<AudioSource> _audioSource = new List<AudioSource>();
    private int _audioToUse = 0;


    //Inspector assigned
    [SerializeField] private float _walkSpeed = 2.0f;
    [SerializeField] private float _runSpeed = 4.5f;
    [SerializeField] private float _jumpSpeed = 7.5f;
    [SerializeField] private float _crouchSpeed = 1.0f;
    [SerializeField] private float _stickToGroundForce = 5.0f;
    [SerializeField] private float _gravityMultiplier = 2.5f;
    [SerializeField] private float _runStepLength = 0.75f;
    [SerializeField] private CurveControlledBob _headBob = new CurveControlledBob();
    [SerializeField] private GameObject _flashLight = null;

    //used standardassets for mouse look
    [SerializeField] private UnityStandardAssets.Characters.FirstPerson.MouseLook _mouseLook;

    //Private
    private Camera _camera = null;
    private bool _jumpButtonPressed = false;
    private Vector2 _inputVector = Vector2.zero;
    private Vector3 _moveDirection = Vector3.zero;
    private bool _previouslyGrounded = false;
    private bool _isWalking = true;
    private bool _isJumping = false;
    private bool _isCrouching = false;
    private Vector3 _localSpaceCameraPos = Vector3.zero;
    private float _controllerHeight = 0.0f;

    //Timers
    private float _fallingTimer = 0.0f;

    private CharacterController _characterController = null;
    private PlayerMoveStatus _movementStatus = PlayerMoveStatus.NOTMOVING;

    //Public
    public PlayerMoveStatus movementStatus { get { return _movementStatus; } }
    public float walkSpeed { get { return _walkSpeed; } }
    public float runSpeed { get { return _runSpeed; } }

    //functions

    protected void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _controllerHeight = _characterController.height;

        //searches for camera with main camera tag
        _camera = Camera.main;
        _localSpaceCameraPos = _camera.transform.localPosition;
        print(_localSpaceCameraPos);

        _movementStatus = PlayerMoveStatus.NOTMOVING;

        _fallingTimer = 0.0f;

        _mouseLook.Init(transform, _camera.transform);

        _headBob.Initialize();
        _headBob.RegisterEventCallback(1.5f, PlayFootstepSound, CurveControlledBobCallbackType.VERTICAL);

        if (_flashLight) _flashLight.SetActive(false);
    }

    protected void Update()
    {
        //falling increment timer
        if (_characterController.isGrounded) _fallingTimer = 0.0f;
        else _fallingTimer += Time.deltaTime;

        //allow time for mouse movement, game isnt paused
        if (Time.timeScale > Mathf.Epsilon)
            _mouseLook.LookRotation(transform, _camera.transform);

        //flashlight
        if (Input.GetButtonDown("Flashlight"))
        {
            if (_flashLight)
                _flashLight.SetActive(!_flashLight.activeSelf);
        }

        //jump
        if (!_jumpButtonPressed && !_isCrouching)
            _jumpButtonPressed = Input.GetButtonDown("Jump");

        //Crouching
        if (Input.GetButtonDown("Crouch"))
        {
            _isCrouching = !_isCrouching;
            _characterController.height = _isCrouching == true ? _controllerHeight / 2.0f : _controllerHeight;
        }

        //jump status
        if (!_previouslyGrounded && _characterController.isGrounded)
        {
            if (_fallingTimer > 0.5f)
            {
                //TODO play landing sound
            }

            _moveDirection.y = 0f;
            _isJumping = false;
            _movementStatus = PlayerMoveStatus.LANDING;
        }
        else
        if (!_characterController.isGrounded)
            _movementStatus = PlayerMoveStatus.NOTGROUNDED;
        else
        if (_characterController.velocity.sqrMagnitude < 0.01f)
            _movementStatus = PlayerMoveStatus.NOTMOVING;
        else
        if (_isCrouching)
            _movementStatus = PlayerMoveStatus.CROUCHING;
        else
        if (_isWalking)
            _movementStatus = PlayerMoveStatus.WALKING;
        else
            _movementStatus = PlayerMoveStatus.RUNNING;

        _previouslyGrounded = _characterController.isGrounded;
    }

    protected void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool wasWalking = _isWalking;
        _isWalking = !Input.GetKey(KeyCode.LeftShift);

        float speed = _isCrouching ? _crouchSpeed : _isWalking ? _walkSpeed : _runSpeed;
        _inputVector = new Vector2(horizontal, vertical);

        if (_inputVector.sqrMagnitude > 1) _inputVector.Normalize();
        //direction we want to move
        Vector3 desiredMove = transform.forward * _inputVector.y + transform.right * _inputVector.x;

        //get a normal for the suface that is being touched to move along it 
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, _characterController.radius, Vector3.down, out hitInfo, _characterController.height / 2f, 1))
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

        _moveDirection.x = desiredMove.x * speed;
        _moveDirection.z = desiredMove.z * speed;

        if (_characterController.isGrounded)
        {
            //keeping grounded
            _moveDirection.y = -_stickToGroundForce;

            if (_jumpButtonPressed)
            {
                _moveDirection.y = _jumpSpeed;
                _jumpButtonPressed = false;
                _isJumping = true;
            }
        }
        else
        {
            //not on ground
            _moveDirection += Physics.gravity * _gravityMultiplier * Time.fixedDeltaTime;
        }

        //move character
        Vector3 speedXZ = new Vector3(_characterController.velocity.x, 0.0f, _characterController.velocity.z);
        _characterController.Move(_moveDirection * Time.fixedDeltaTime);

        if (speedXZ.magnitude > 0.01f)
            _camera.transform.localPosition = _localSpaceCameraPos + _headBob.GetVectorOffset(speedXZ.magnitude * (_isCrouching || _isWalking?1.0f:_runStepLength));        
        else
            _camera.transform.localPosition = _localSpaceCameraPos;
    }

    //temp
    void PlayFootstepSound()
    {
        if (_isCrouching) return;

        _audioSource[_audioToUse].Play();
        _audioToUse = (_audioToUse == 0) ? 1 : 0;
    }

}
