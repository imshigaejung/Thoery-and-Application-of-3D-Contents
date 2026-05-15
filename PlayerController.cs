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

    // 파티클
    public ParticleSystem boosterEffect;
    public GameObject explosionPrefab;

    // 래그돌
    public float ragdollDuration = 2f;
    private float ragdollTimer;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private float rollInput;
    private Vector3 velocity;
    public float acceleration = 8f;
    public float deceleration = 8f;
    public float driftControl = 2f; // 드리프트 중 방향 보정 정도

    void Awake()
    {
        inputActions = new PlayerInputActions();
        rb = GetComponent<Rigidbody>();
        boosterEffect.Stop();
    }
    void Start()
    {
        currentFuel = maxFuel;

        // 래그돌로 자세 정렬
        EnterRagdoll();
        RecoverFromRagdoll();
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
        
        Debug.Log("IsGrounded : "+IsGrounded());
        Debug.Log("IsUpright : "+IsUpright());
        Debug.Log("currentState : "+currentState);
        
        
        if (currentState == PlayerState.Normal)
        {
            if (IsGrounded()&&IsUpright())
            {
                Move();
                Rotate();
            }
            else if (!IsGrounded())
            {
                Roll();
            }

        }   
    }
 
    void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;

        inputActions.Player.Roll.performed += OnRoll;
        inputActions.Player.Roll.canceled += OnRoll;

        inputActions.Player.Interact.performed += OnInteract;
    }
    void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;

        inputActions.Player.Roll.performed -= OnRoll;
        inputActions.Player.Roll.canceled -= OnRoll;

        inputActions.Player.Interact.performed -= OnInteract;

        inputActions.Disable();
    }
    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("상호작용!");
        // 여기서 아이템 줍기 or 인벤토리 연결
    }
    void OnRoll(InputAction.CallbackContext context)
    {
        rollInput = context.ReadValue<float>();
    }
    void OnCollisionEnter()
    {
        float speed = rb.linearVelocity.magnitude;
        Debug.Log("speed :"+speed);
        if(speed > 50)
        {
            GameObject explosionEffect = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            explosionEffect.GetComponent<ExplosionController>().PlayAll();
        }
    }
/*
    void Move()
    {
        float targetSpeed = isDrifting ? driftSpeed : normalSpeed;

        // 1. 중력을 방해하지 않기 위해 XZ 평면(바닥) 기준의 현재 속도만 가져옵니다.
        Vector3 currentVelocityXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        // 현재 이동 방향 (속도가 너무 느리면 앞을 보고 있다고 가정)
        Vector3 currentDir = currentVelocityXZ.sqrMagnitude > 0.01f 
            ? currentVelocityXZ.normalized 
            : transform.forward;

        // 플레이어 입력에 따른 목표 방향
        Vector3 desiredDirection = transform.forward * moveInput.y;
        Vector3 desiredVelocityXZ = Vector3.zero;

        // 입력이 들어오면 가속
        if (moveInput.y != 0)
        {
            if (isDrifting)
            {
                desiredDirection = Vector3.MoveTowards(
                    currentDir,
                    desiredDirection,
                    driftControl * Time.fixedDeltaTime
                );
            }
            // 가속
            desiredVelocityXZ = Vector3.MoveTowards(
                velocity,
                desiredDirection * targetSpeed,
                acceleration * Time.deltaTime
            );

            // 드리프트 중 방향 보정 약하게
            if (isDrifting)
            {
                Vector3 driftDirection = Vector3.MoveTowards(
                    velocity.normalized,
                    desiredDirection,
                    driftControl * Time.deltaTime
                );

                velocity = driftDirection.normalized * velocity.magnitude;
            }
            else
            {
                // 일반 상태에서는 방향 빠르게 맞춤
                velocity = Vector3.MoveTowards(
                    velocity,
                    desiredDirection * velocity.magnitude,
                    10f * Time.deltaTime
                );
            }
        }
        // 입력이 없으면 감속
        else
        {
            // 감속 (자연스럽게 멈춤)
            velocity = Vector3.MoveTowards(
                velocity,
                Vector3.zero,
                deceleration * Time.deltaTime
            );
        }

        transform.position += velocity * Time.deltaTime;
    }
    */
    void Move()
    {
        float targetSpeed = isDrifting ? driftSpeed : normalSpeed;

        // 1. 중력을 방해하지 않기 위해 XZ 평면(바닥) 기준의 현재 속도만 가져옵니다.
        Vector3 currentVelocityXZ = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        // 현재 이동 방향 
        Vector3 currentDir = currentVelocityXZ.sqrMagnitude > 0.01f 
            ? currentVelocityXZ.normalized 
            : Vector3.zero;

        // 플레이어 입력에 따른 목표 방향
        Vector3 desiredDirection = transform.forward * moveInput.y;
        Vector3 desiredVelocityXZ = Vector3.zero;

        if (moveInput.y != 0)
        {
            // 2. 상태에 따른 회전 속도 결정
            float turnSpeed = isDrifting ? 10f + driftControl : 10f;

            // 3. 현재 이동 방향에서 목표 방향으로 자연스럽게 회전 (보정)
            Vector3 alignedDirection = Vector3.MoveTowards(
                currentDir, 
                desiredDirection, 
                turnSpeed * Time.fixedDeltaTime
            );

            // 핵심 수정: 현재 속도(magnitude)가 아니라, '목표 속도(targetSpeed)'를 곱해줍니다!
            desiredVelocityXZ = alignedDirection.normalized * targetSpeed;

            // 4. 속도 차이(오차) 계산 (Y축 제외)
            Vector3 velocityDiff = desiredVelocityXZ - currentVelocityXZ;

            // 5. AddForce로 차이만큼 힘을 가해 목표 속도에 도달하게 함
            // ForceMode.Acceleration은 질량을 무시하므로 가속도(acceleration) 값을 직관적으로 쓰기 좋습니다.
            rb.AddForce(velocityDiff * acceleration, ForceMode.Acceleration);
        }
        else
        {
            // 입력이 없으면 목표 속도는 0 (감속)
            desiredVelocityXZ = Vector3.zero;

            // 4. 속도 차이(오차) 계산 (Y축 제외)
            Vector3 velocityDiff = desiredVelocityXZ - currentVelocityXZ;

            rb.AddForce(velocityDiff * deceleration, ForceMode.Acceleration);
        }

        
        
    }
    void Rotate()
    {
        float rotSpeed = isDrifting ? driftRotationSpeed : normalRotationSpeed;
        transform.Rotate(Vector3.up * moveInput.x * rotSpeed * Time.deltaTime);
    }
    void Roll()
    {
        float rotSpeed = flyTurnSpeed;
                // 🎮 WASD → 방향 조작 (에임 느낌)
        float yaw = moveInput.x * flyTurnSpeed * Time.deltaTime;
        float pitch = -moveInput.y * flyTurnSpeed * Time.deltaTime;
        float roll = rollInput * flyTurnSpeed * Time.deltaTime;
    
        transform.Rotate(pitch, yaw, roll);
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

        // pitch, yaw, roll로 조종
        Roll();

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
        return Physics.CheckSphere(
            transform.position + Vector3.down * 0.5f, // 발 위치
            0.3f,
            LayerMask.GetMask("Ground")
        );
    }
    bool IsUpright()
    {
        return Vector3.Angle(transform.up, Vector3.up) < 45f;
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

}