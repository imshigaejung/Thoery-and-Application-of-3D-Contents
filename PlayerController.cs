using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    enum PlayerState
    {
        Normal,
        Flying,
        Ragdoll
    }

    private PlayerState currentState = PlayerState.Normal;
    // 직진 물리량
    public float normalSpeed = 3f;
    public float driftSpeed = 6f;
    // 회전 물리량
    public float normalRotationSpeed = 100f;
    public float driftRotationSpeed = 180f;
    private bool isDrifting;
    // 비행 물리량
    public float flySpeed = 10f;
    public float flyTurnSpeed = 60f;
    public float gravity = -9.8f;
    // 연료
    public float maxFuel = 5f;
    private float currentFuel;
    public float fuelBurnRate = 1f;
    public float fuelRegenRate = 1.5f;

    // 부스터 파티클
    public ParticleSystem boosterEffect;
    public float ragdollDuration = 2f;
    private float ragdollTimer;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector3 velocity;
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float driftControl = 2f; // 드리프트 중 방향 보정 정도

    void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        inputActions.Player.Interact.performed += OnInteract;
    }

    void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        inputActions.Player.Interact.performed -= OnInteract;

        inputActions.Disable();
    }
    void Start()
    {
        currentFuel = maxFuel;
    }
    void Update()
    {
        HandleRagdollInput();
        HandleRagdollRecovery(); // 🔥 자동 복구

        if (currentState == PlayerState.Ragdoll)
            return;
        
        if (currentState == PlayerState.Normal)
        {
            HandleDriftState();
            HandleBoostInput();
            HandleFuel();
        }
        else if (currentState == PlayerState.Flying)
        {
            Fly();
        }
    }
    void FixedUpdate()
    {
        if (currentState == PlayerState.Normal)
        {
            Move();
            Rotate();
        }   
    }
    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Move()
    {
        float targetSpeed = isDrifting ? driftSpeed : normalSpeed;

        // 목표 이동 방향 (플레이어가 보는 방향 기준)
        Vector3 desiredDirection = transform.forward * moveInput.y;

        Vector3 desiredVelocity;

        if (moveInput.y != 0)
        {
            // 가속
            desiredVelocity = desiredDirection * targetSpeed;
        }
        else
        {
            // 감속 (자연스럽게 멈춤)
            desiredVelocity = Vector3.zero;
        }

        // 현재 속도와 목표 속도 차이 계산
        Vector3 velocityDiff = desiredVelocity - rb.velocity;

        // 드리프트 중 방향 보정
        if (isDrifting)
        {
            Vector3 driftDirection = Vector3.Lerp(
                rb.velocity.normalized,
                desiredDirection,
                driftControl * Time.fixedDeltaTime
            );

            desiredVelocity = driftDirection.normalized * rb.velocity.magnitude;
            velocityDiff = desiredVelocity - rb.velocity;
        }
        else
        {
            // 일반 상태에서는 방향 빠르게 맞춤
            Vector3 alignedDirection = Vector3.Lerp(
                rb.velocity.normalized,
                desiredDirection,
                10f * Time.fixedDeltaTime
            );

            desiredVelocity = alignedDirection.normalized * rb.velocity.magnitude;
            velocityDiff = desiredVelocity - rb.velocity;
        }

        // AddForce로 물리 기반 이동
        rb.AddForce(velocityDiff * acceleration, ForceMode.Acceleration);
    }
    /*{
        float targetSpeed = isDrifting ? driftSpeed : normalSpeed;

        // 목표 이동 방향 (플레이어가 보는 방향 기준)
        Vector3 desiredDirection = transform.forward * moveInput.y;

        if (moveInput.y != 0)
        {
            // 가속
            velocity = Vector3.Lerp(
                velocity,
                desiredDirection * targetSpeed,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            // 감속 (자연스럽게 멈춤)
            velocity = Vector3.Lerp(
                velocity,
                Vector3.zero,
                deceleration * Time.deltaTime
            );
        }

        // 🔥 드리프트 중 방향 보정 약하게
        if (isDrifting)
        {
            Vector3 driftDirection = Vector3.Lerp(
                velocity.normalized,
                desiredDirection,
                driftControl * Time.deltaTime
            );

            velocity = driftDirection.normalized * velocity.magnitude;
        }
        else
        {
            // 일반 상태에서는 방향 빠르게 맞춤
            velocity = Vector3.Lerp(
                velocity,
                desiredDirection * velocity.magnitude,
                10f * Time.deltaTime
            );
        }

        transform.position += velocity * Time.deltaTime;
    }*/
    void Rotate()
    {
        float rotSpeed = isDrifting ? driftRotationSpeed : normalRotationSpeed;
        transform.Rotate(Vector3.up * moveInput.x * rotSpeed * Time.deltaTime);
    }

    public bool IsDrifting()
    {
        return isDrifting;
    }
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    // 이벤트 헨들링
    void HandleDriftState()
    {
        bool moving = Mathf.Abs(moveInput.y) > 0.1f;
        bool rotating = Mathf.Abs(moveInput.x) > 0.1f;
        bool shift = Keyboard.current.leftShiftKey.isPressed;

        isDrifting = shift && moving && rotating;
    }
    void HandleBoostInput()
    {
        if (Keyboard.current.vKey.wasPressedThisFrame && currentFuel > 0)
        {
            EnterFlyMode();
        }
    }
    void HandleRagdollInput()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (currentState == PlayerState.Ragdoll)
                RecoverFromRagdoll();
            else
                EnterRagdoll();
        }
    }
    void HandleRagdollRecovery()
    {
        if (currentState != PlayerState.Ragdoll) return;

        ragdollTimer -= Time.deltaTime;

        if (ragdollTimer <= 0f)
        {
            RecoverFromRagdoll();
        }
    }

    // 비행 시스템
    void EnterFlyMode()
    {
        currentState = PlayerState.Flying;
        rb.useGravity = false;

        boosterEffect.Play();
    }
    void Fly()
    {
        // 연료 소모
        currentFuel -= fuelBurnRate * Time.deltaTime;

        if (currentFuel <= 0)
        {
            ExitFlyMode();
            return;
        }

        // 🎮 WASD → 방향 조작 (에임 느낌)
        float yaw = moveInput.x * flyTurnSpeed * Time.deltaTime;
        float pitch = -moveInput.y * flyTurnSpeed * Time.deltaTime;

        transform.Rotate(pitch, yaw, 0);

        // 👉 전진 속도 고정
        Vector3 forwardMove = transform.forward * flySpeed;

        rb.linearVelocity = forwardMove;
    }
    void ExitFlyMode()
    {
        currentState = PlayerState.Normal;

        rb.useGravity = true; // 🔥 다시 중력 켜기

        boosterEffect.Stop();
    }

    bool IsGrounded()
    {
        return Physics.SphereCast(
            transform.position,
            0.3f,
            Vector3.down,
            out RaycastHit hit,
            1.2f
        );
    }
    public float GetFuelPercent()
{
    return currentFuel / maxFuel;
}
    void HandleFuel()
    {
        currentFuel += fuelRegenRate * Time.deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
    }
  
   // 래그돌
   void EnterRagdoll()
    {
        currentState = PlayerState.Ragdoll;

        // 이동 멈춤
        rb.linearVelocity = Vector3.zero;

        // 회전 제한 해제 (넘어질 수 있게)
        rb.constraints = RigidbodyConstraints.None;
        Debug.Log("current state :" + currentState);
    }
    void RecoverFromRagdoll()
    {
        currentState = PlayerState.Normal;

        // 회전 초기화 (세우기)
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, euler.y, 0);

        // 회전 잠그기
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 회전 튀는거 방지
        rb.angularVelocity = Vector3.zero;
        Debug.Log("current state :" + currentState);
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("상호작용!");
        // 여기서 아이템 줍기 or 인벤토리 연결
    }
}