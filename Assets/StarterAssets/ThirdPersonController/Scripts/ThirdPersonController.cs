 using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Addition")]
        [SerializeField] public GameObject maskModel;

        [Header("Frog Bounce Settings")]
        public Transform visualModel; // Assign the 3D model child object here
        public float bounceFrequency = 10f; // How fast the hops are
        public float bounceAmplitude = 0.2f; // How high the hops are
        private float _bounceTimer;

        [Header("Frog Cooldown")]
        public float hopCooldownDuration = 0.2f; // How long to stay still after landing
        private float _cooldownTimer = 0f;
        private bool _isOnCooldown = false;

        [Header("Slingshot Leap")]
        public LineRenderer jumpLine;
        public float maxLeapForce = 20f;
        public float leapChargeSpeed = 1.5f;
        public float jumpAimSensitivity = 100f;
        [Tooltip("Optional: a Transform (eg. small marker) that will be placed at the predicted landing point while aiming")]
        public Transform landingMarker;
        [Tooltip("Layers considered 'enemies' for collision debugging while mid-leap")]
        public LayerMask EnemyLayers;


        private bool _isCharging = false;
        private float _currentCharge = 0f;
        private float _leapForwardSpeed = 0f; // Stores the horizontal force of the leap
        private float _jumpAimAngle;
        // Using `_isCharging` to track whether we're in the aim/charge state.
        private Vector3 _leapDirection = Vector3.forward; // Horizontal direction used during leap
        private Vector3 _leapVelocity = Vector3.zero; // Horizontal velocity applied while airborne during a leap
        private Vector3 _predictedLandingPoint = Vector3.zero;

        // Public read-only access to the last predicted landing point
        public Vector3 PredictedLandingPoint => _predictedLandingPoint;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // bounce logic
        private Vector3 _initialModelPosition;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            //bounce logic
            if (visualModel != null)
            {
                _initialModelPosition = visualModel.localPosition;
            }
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            //Move();
            HandleLeapInput();
    
                // Only run normal Move() if we aren't charging a jump
            if (!_isCharging)
            {
                Move();
            }
            else
            {
                // Ensure landing marker is active while charging (DrawTrajectory sets position)
                if (landingMarker != null) landingMarker.gameObject.SetActive(true);
            }
            // Hide landing marker when not charging
            if (!_isCharging && landingMarker != null)
            {
                landingMarker.gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            // --- 1. COOLDOWN CLOCK ---
            if (_isOnCooldown)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0)
                {
                    _isOnCooldown = false;
                    _bounceTimer = 0; // Reset hop timer for a fresh jump
                }
            }

            // Inside Move() function

            

            // --- 2. SPEED CALCULATION ---
            // If on cooldown, target speed is ALWAYS 0
            float targetSpeed = (_input.sprint ? SprintSpeed : MoveSpeed);
            if (_input.move == Vector2.zero || _isOnCooldown) targetSpeed = 0.0f;

            // If we are aiming/charging the frog leap, don't run any movement logic
            if (_isCharging) 
            {
                // Optional: Ensure animator stays in Idle
                if (_hasAnimator) _animator.SetFloat(_animIDSpeed, 0);
                return; 
            }

            // If we are charging, or if we just leaped and are in the air, override the speed
            if (_isCharging) 
            {
                targetSpeed = 0.0f; // Stay still while aiming
            }
            else if (!Grounded)
            {
                targetSpeed = _leapForwardSpeed; // Keep the leap momentum while in the air
            }
            else if (_input.move == Vector2.zero)
            {
                targetSpeed = 0.0f;
            }

            // [Keep your existing acceleration/lerp code here...]
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                //_speed = targetSpeed;
                // If we are in the air from a leap, keep our leap speed!
                targetSpeed = Grounded ? (_input.sprint ? SprintSpeed : MoveSpeed) : _leapForwardSpeed;

                if (Grounded && _input.move == Vector2.zero) targetSpeed = 0.0f;
            }

            // --- 3. UPDATED BOUNCE & LANDING LOGIC ---
            if (_speed > 0.1f && _controller.isGrounded && !_isOnCooldown) 
            {
                _bounceTimer += Time.deltaTime * bounceFrequency;
                
                // Use Sin instead of Abs(Sin) to easily detect the end of one hop (PI)
                float sineValue = Mathf.Sin(_bounceTimer);
                
                // If the sine wave goes below zero, the hop has finished one cycle
                if (sineValue < 0)
                {
                    _isOnCooldown = true;
                    _cooldownTimer = hopCooldownDuration;
                    visualModel.localPosition = _initialModelPosition;
                }
                else
                {
                    float bounceOffset = sineValue * bounceAmplitude;
                    visualModel.localPosition = _initialModelPosition + new Vector3(0, bounceOffset, 0);
                }
            }
            else if (visualModel != null && !_isOnCooldown)
            {
                visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, _initialModelPosition, Time.deltaTime * 10f);
            }
            
            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // If a leap horizontal velocity is stored, use it so motion matches the trajectory preview (ballistic motion)
            if (_leapVelocity.sqrMagnitude > 0.0001f)
            {
                _controller.Move(_leapVelocity * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
            else
            {
                // Otherwise use normal movement direction
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                                 new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void HandleLeapInput()
        {
            // Only allow initiating or adjusting a leap while grounded
            if (!Grounded) return;

            // Toggle/consume jump input
            if (_input.jump)
            {
                _input.jump = false; // consume

                if (!_isCharging)
                {
                    // Enter aim/charge mode
                    _isCharging = true;
                    _currentCharge = 0.3f; // start charge
                    if (jumpLine != null) jumpLine.enabled = true;
                    _leapForwardSpeed = 0f;
                    _speed = 0f;
                    _animationBlend = 0f;
                    _verticalVelocity = -2f; // cancel downward velocity
                    // initialize aim direction/rotation to current transform so visuals and motion match immediately
                    _targetRotation = transform.eulerAngles.y;
                    _leapDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                }
                else
                {
                    // Second press executes the leap
                    ExecuteLeap();
                    return;
                }
            }

            // While charging/aiming, allow rotation and adjusting power
            if (_isCharging)
            {
                // Aiming rotation with horizontal input
                float targetRotation = _input.move.x * jumpAimSensitivity * Time.deltaTime;
                transform.Rotate(0, targetRotation, 0);

                // Keep _targetRotation synced with transform when aiming so movement direction matches visuals
                _targetRotation = transform.eulerAngles.y;
                _leapDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

                // Use vertical input to adjust charge power
                _currentCharge = Mathf.Clamp(_currentCharge + (_input.move.y * Time.deltaTime), 0.1f, 1.0f);

                DrawTrajectory();
            }
        }

        private void ExecuteLeap()
        {
            // 1. Reset States
            _isCharging = false;
            if (jumpLine != null) jumpLine.enabled = false;
            // Mark as airborne so other systems consider the player in the air immediately
            Grounded = false;
            if (landingMarker != null) landingMarker.gameObject.SetActive(false);

            // 2. Calculate and Apply Forces
            float power = _currentCharge * maxLeapForce;
            _verticalVelocity = power * 0.7f; 
            _leapForwardSpeed = power; 
            _speed = _leapForwardSpeed;
            // Capture the horizontal direction at the moment of leap so Move() uses the aimed direction
            _leapDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            // Store horizontal velocity vector (ballistic horizontal velocity) so the character follows the drawn trajectory
            _leapVelocity = _leapDirection * power;

            // 3. FORCE RESET INPUTS (Crucial for Starter Assets)
            _input.jump = false; 
            _currentCharge = 0f;

            // 4. Animation
            if (_hasAnimator) 
            {
                _animator.SetBool(_animIDJump, true);
                _animator.SetBool(_animIDFreeFall, false);
            }
        }

        private void DrawTrajectory()
        {
            if (jumpLine == null) return;

            // Use the same initial velocity calculation as ExecuteLeap, and use this script's Gravity
            Vector3 startPos = transform.position + (transform.up * 0.5f);
            float power = _currentCharge * maxLeapForce;
            Vector3 horizDir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 initialVelocity = horizDir * power + Vector3.up * (power * 0.7f);
            Vector3 gravityVec = Vector3.up * Gravity; // Gravity is negative in this script

            int maxPoints = 60;
            float timeStep = 0.05f; // 20 Hz sampling for a smooth, reasonably accurate curve

            var pts = new System.Collections.Generic.List<Vector3>(maxPoints);
            pts.Add(startPos);

            // Simulate trajectory and stop on first collision with GroundLayers
            for (int i = 1; i < maxPoints; i++)
            {
                float t = i * timeStep;
                Vector3 point = startPos + initialVelocity * t + 0.5f * gravityVec * t * t;

                // Raycast between last point and this point to detect collisions and stop the preview where the character would hit
                Vector3 last = pts[pts.Count - 1];
                Vector3 dir = point - last;
                float dist = dir.magnitude;
                if (dist > 0.0001f)
                {
                    // Use a sphere cast rather than a raycast so thin/low geometry and start-inside cases are handled
                    float castRadius = GroundedRadius;
                    if (_controller != null)
                    {
                        // CharacterController.radius is the best available proxy for the player's horizontal size
                        castRadius = Mathf.Max(0.01f, _controller.radius);
                    }

                    if (Physics.SphereCast(last, castRadius, dir.normalized, out RaycastHit hit, dist, GroundLayers, QueryTriggerInteraction.Ignore))
                    {
                        pts.Add(hit.point);
                        break;
                    }
                }

                pts.Add(point);

                // If we've gone well below the starting height and aren't hitting anything, we can stop early
                if (point.y < startPos.y - 50f) break;
            }

            jumpLine.positionCount = pts.Count;
            for (int i = 0; i < pts.Count; i++) jumpLine.SetPosition(i, pts[i]);

            // Store and optionally display the predicted landing point
            _predictedLandingPoint = pts[pts.Count - 1];
            if (landingMarker != null)
            {
                landingMarker.position = _predictedLandingPoint;
                landingMarker.gameObject.SetActive(true);
            }
}
        
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // Stop velocity dropping, but ONLY if we aren't about to leap
                if (_verticalVelocity < 0.0f && !_isCharging)
                {
                    _verticalVelocity = -2f;
                }

                // Clear stored leap horizontal velocity only when the CharacterController actually reports grounded.
                // This avoids clearing it mid-air when the `Grounded` flag may have been set by a proximity check.
                if (_leapVelocity.sqrMagnitude > 0.0001f && _controller != null && _controller.isGrounded)
                {
                    _leapVelocity = Vector3.zero;
                }


                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
                }

                //_input.jump = false;
            }

    // Apply gravity (Don't apply while charging or we might slide)
    if (!_isCharging && _verticalVelocity > -15f)
    {
        _verticalVelocity += Gravity * Time.deltaTime;
    }
}
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Only debug/report collisions while we have an active leap horizontal velocity
            if (_leapVelocity.sqrMagnitude <= 0.0001f) return;

            // Check if the hit collider is on the enemy layers
            if (((1 << hit.gameObject.layer) & EnemyLayers.value) != 0)
            {
                Vector3 hitPoint = hit.point;
                Debug.Log($"[Leap Debug] Collided with enemy '{hit.gameObject.name}' at {hitPoint} (normal: {hit.normal}). _leapVelocity: {_leapVelocity}");

                // Draw a short-lived debug line and sphere to visualise the collision in the Scene view
                Debug.DrawLine(transform.position, hitPoint, Color.red, 2.0f);
                Debug.DrawRay(hitPoint, hit.normal * 0.5f, Color.yellow, 2.0f);

                // Optional: stop horizontal leap motion on hit so it's easy to observe
                // _leapVelocity = Vector3.zero;
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}