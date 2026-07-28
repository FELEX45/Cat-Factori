using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

[RequireComponent(typeof(CharacterController))]
public class move : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;

    [Header("Animations")]
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip runClip;
    public AnimationClip walkBackClip;
    public AnimationClip strafeLeftWalkClip;
    public AnimationClip strafeRightWalkClip;
    public AnimationClip strafeLeftRunClip;
    public AnimationClip strafeRightRunClip;
    public AnimationClip jumpClip;
    public AnimationClip danceClip;

    [Header("First Person Look")]
    public float mouseSensitivity = 0.12f;
    public float gamepadLookSpeed = 120f;
    public float eyeHeight = 0.7f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Jump Animation Sync")]
    [Tooltip("На какой высоте ног над землёй (м) начинать кадры приземления")]
    public float jumpLandDistance = 0.25f;
    [Tooltip("Доля клипа: поза в воздухе (без касания земли)")]
    [Range(0.35f, 0.75f)]
    public float jumpAirborneClipEnd = 0.62f;
    [Tooltip("Доля клипа: с какого кадра идёт удар ногами о землю")]
    [Range(0.6f, 0.95f)]
    public float jumpLandClipStart = 0.82f;

    CharacterController controller;
    Animator animator;
    Transform cam;
    Vector3 velocity;
    float pitch;
    bool isDancing;
    bool jumpAnimPlaying;
    float jumpStartY;

    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable[] playables;
    AnimationClip[] clips;
    bool graphReady;
    int currentSlot = -1;

    const int SlotIdle = 0;
    const int SlotWalk = 1;
    const int SlotRun = 2;
    const int SlotWalkBack = 3;
    const int SlotStrafeLeftWalk = 4;
    const int SlotStrafeRightWalk = 5;
    const int SlotStrafeLeftRun = 6;
    const int SlotStrafeRightRun = 7;
    const int SlotJump = 8;
    const int SlotDance = 9;
    const int SlotCount = 10;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController = null;

        SetupPlayables();
        HidePlayerBodyFromFirstPersonCamera();

        if (cam == null)
            BindSceneCamera();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Привязывает камеру активной сцены к игроку. Не уничтожает уже нужную Main Camera.
    /// </summary>
    public void BindSceneCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            {
                if (c != null && c.enabled && c.gameObject.scene.IsValid()
                    && c.gameObject.scene.name == LobbySessionManager.GameSceneName)
                {
                    mainCam = c;
                    break;
                }
            }
        }

        // Уже привязана правильно — не трогаем (иначе Start() убьёт камеру после SetupLocalPlayer)
        if (cam != null && mainCam != null && cam == mainCam.transform && cam.parent == transform)
        {
            EnsureSingleAudioListener(mainCam);
            return;
        }

        // Убрать только чужие/старые камеры под игроком (не трогая mainCam)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            var childCam = child.GetComponent<Camera>();
            if (childCam == null)
                continue;
            if (mainCam != null && childCam == mainCam)
                continue;
            Destroy(child.gameObject);
        }

        if (mainCam == null)
            mainCam = CreateFallbackCamera();

        if (mainCam == null)
        {
            Debug.LogError("[move] Нет камеры для привязки");
            return;
        }

        if (!mainCam.CompareTag("MainCamera"))
            mainCam.tag = "MainCamera";

        EnsureSingleAudioListener(mainCam);

        cam = mainCam.transform;
        cam.SetParent(transform, false);
        cam.localPosition = new Vector3(0f, eyeHeight, 0f);
        cam.localRotation = Quaternion.identity;
        pitch = 0f;
        mainCam.enabled = true;
    }

    Camera CreateFallbackCamera()
    {
        var go = new GameObject("PlayerCamera");
        var created = go.AddComponent<Camera>();
        created.tag = "MainCamera";
        created.nearClipPlane = 0.05f;
        go.AddComponent<AudioListener>();
        Debug.LogWarning("[move] Создана запасная PlayerCamera");
        return created;
    }

    static void EnsureSingleAudioListener(Camera keep)
    {
        var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        foreach (var listener in listeners)
        {
            if (listener == null)
                continue;
            if (keep != null && listener.gameObject == keep.gameObject)
            {
                listener.enabled = true;
                continue;
            }
            Destroy(listener);
        }

        if (keep != null && keep.GetComponent<AudioListener>() == null)
            keep.gameObject.AddComponent<AudioListener>();
    }

    void HidePlayerBodyFromFirstPersonCamera()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer < 0)
            playerLayer = 6;

        SetLayerRecursively(transform, playerLayer);

        Camera mainCam = Camera.main;
        if (mainCam != null)
            mainCam.cullingMask &= ~(1 << playerLayer);
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }

    void OnDisable()
    {
        if (graphReady && graph.IsValid())
            graph.Stop();
    }

    void OnEnable()
    {
        if (graphReady && graph.IsValid())
            graph.Play();
    }

    void OnDestroy()
    {
        if (graphReady && graph.IsValid())
            graph.Destroy();
    }

    void SetupPlayables()
    {
        clips = new[]
        {
            idleClip,
            walkClip,
            runClip,
            walkBackClip,
            strafeLeftWalkClip,
            strafeRightWalkClip,
            strafeLeftRunClip,
            strafeRightRunClip,
            jumpClip,
            danceClip
        };

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
            {
                Debug.LogError($"Не назначен клип слота {i}. Нажми кнопку загрузки анимаций на move.");
                return;
            }
        }

        graph = PlayableGraph.Create("PlayerAnims");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        mixer = AnimationMixerPlayable.Create(graph, SlotCount);
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        output.SetSourcePlayable(mixer);

        playables = new AnimationClipPlayable[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            playables[i] = CreateLoopingClip(clips[i]);
            mixer.ConnectInput(i, playables[i], 0);
            mixer.SetInputWeight(i, i == SlotIdle ? 1f : 0f);
        }

        graph.Play();
        graphReady = true;
        currentSlot = SlotIdle;
        Restart(playables[SlotIdle]);
    }

    AnimationClipPlayable CreateLoopingClip(AnimationClip clip)
    {
        var playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(false);
        playable.SetSpeed(1.0);
        playable.SetTime(0);
        playable.Play();
        return playable;
    }

    void Update()
    {
        if (cam == null)
            BindSceneCamera();

        // Пока открыт чат или пауза — не двигаемся
        if (ChatHud.Instance != null && ChatHud.Instance.IsOpen)
            return;
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
            return;
        if (GameplayHud.BlocksWorldInput)
            return;

        HandleCursor();
        HandleDanceInput();
        HandleLook();
        HandleMovement();
        UpdateAnimation();
    }

    void HandleCursor()
    {
        // ESC обрабатывает PauseMenu; ЛКМ возвращает захват мыши
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked
            && !GameplayHud.BlocksWorldInput
            && (ChatHud.Instance == null || !ChatHud.Instance.IsOpen)
            && (PauseMenu.Instance == null || !PauseMenu.Instance.IsOpen))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleDanceInput()
    {
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
            isDancing = !isDancing;
    }

    void HandleLook()
    {
        if (isDancing || cam == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        float sens = PlayerProfile.MouseSensitivity;
        Vector2 look = Vector2.zero;
        if (Mouse.current != null)
            look += Mouse.current.delta.ReadValue() * sens;
        if (Gamepad.current != null)
            look += Gamepad.current.rightStick.ReadValue() * gamepadLookSpeed * Time.deltaTime;

        transform.Rotate(0f, look.x, 0f);
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        bool grounded = controller.isGrounded;
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector2 input = ReadMoveInput();
        if (isDancing && input.sqrMagnitude > 0.01f)
            isDancing = false;

        if (isDancing)
            input = Vector2.zero;

        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        float speed = IsSprinting() ? runSpeed : walkSpeed;
        controller.Move(moveDir * speed * Time.deltaTime);

        if (!isDancing && WasJumpPressed() && grounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isDancing = false;
            BeginJumpAnimation();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Прыжок закончился при приземлении
        if (jumpAnimPlaying && controller.isGrounded && velocity.y <= 0f)
            jumpAnimPlaying = false;
    }

    void BeginJumpAnimation()
    {
        jumpAnimPlaying = true;
        jumpStartY = transform.position.y;
    }

    void UpdateAnimation()
    {
        if (!graphReady)
            return;

        int desired = ResolveAnimSlot();
        if (desired != currentSlot)
            SwitchTo(desired);

        if (currentSlot == SlotJump)
            UpdateJumpAnimationTime();
        else
        {
            for (int i = 0; i < SlotCount; i++)
                KeepLooping(playables[i], clips[i], currentSlot == i);
        }
    }

    void UpdateJumpAnimationTime()
    {
        AnimationClipPlayable jumpPlayable = playables[SlotJump];
        AnimationClip clip = clips[SlotJump];
        if (clip == null)
            return;

        float feetAboveGround = GetFeetAboveGround();
        float height01 = Mathf.Clamp01((transform.position.y - jumpStartY) / Mathf.Max(0.01f, jumpHeight));
        float progress;

        if (velocity.y >= 0f)
        {
            // Подъём — только начало клипа до воздушной позы
            progress = Mathf.Lerp(0f, jumpAirborneClipEnd * 0.55f, height01);
        }
        else if (feetAboveGround > jumpLandDistance)
        {
            // Ещё далеко от пола — держим «полёт», не доходим до удара ногами
            float peakApprox = Mathf.Max(jumpLandDistance + 0.05f, jumpHeight);
            float t = Mathf.InverseLerp(peakApprox, jumpLandDistance, Mathf.Min(feetAboveGround, peakApprox));
            progress = Mathf.Lerp(jumpAirborneClipEnd * 0.55f, jumpAirborneClipEnd, t);
        }
        else
        {
            // Ноги уже почти у земли — только тут приземление
            float t = Mathf.InverseLerp(jumpLandDistance, 0f, Mathf.Max(0f, feetAboveGround));
            progress = Mathf.Lerp(jumpLandClipStart, 1f, t);
        }

        double time = Mathf.Clamp01(progress) * clip.length;
        if (time >= clip.length)
            time = clip.length - 0.001;

        jumpPlayable.SetTime(time);
        jumpPlayable.SetSpeed(0);
        jumpPlayable.Play();
    }

    float GetFeetAboveGround()
    {
        float feetY = transform.position.y + controller.center.y - controller.height * 0.5f;
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            return Mathf.Max(0f, feetY - hit.point.y);

        return Mathf.Max(0f, transform.position.y - jumpStartY);
    }

    int ResolveAnimSlot()
    {
        if (isDancing)
            return SlotDance;

        if (jumpAnimPlaying)
            return SlotJump;

        Vector2 input = ReadMoveInput();
        if (input.sqrMagnitude < 0.01f)
            return SlotIdle;

        bool sprint = IsSprinting();
        float ax = Mathf.Abs(input.x);
        float ay = Mathf.Abs(input.y);

        if (ay >= ax)
        {
            if (input.y > 0f)
                return sprint ? SlotRun : SlotWalk;
            return SlotWalkBack;
        }

        if (input.x < 0f)
            return sprint ? SlotStrafeLeftRun : SlotStrafeLeftWalk;

        return sprint ? SlotStrafeRightRun : SlotStrafeRightWalk;
    }

    void SwitchTo(int slot)
    {
        currentSlot = slot;
        for (int i = 0; i < SlotCount; i++)
            mixer.SetInputWeight(i, i == slot ? 1f : 0f);
        Restart(playables[slot]);
    }

    static void Restart(AnimationClipPlayable playable)
    {
        playable.SetTime(0);
        playable.SetSpeed(1.0);
        playable.Play();
    }

    static void KeepLooping(AnimationClipPlayable playable, AnimationClip clip, bool active)
    {
        if (!active || clip == null)
            return;

        if (playable.GetPlayState() != PlayState.Playing)
            playable.Play();

        playable.SetSpeed(1.0);

        double length = clip.length;
        if (length <= 0.001)
            return;

        double t = playable.GetTime();
        if (t >= length)
            playable.SetTime(t % length);
    }

    static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
        }
        if (Gamepad.current != null)
            input += Gamepad.current.leftStick.ReadValue();
        return Vector2.ClampMagnitude(input, 1f);
    }

    static bool IsSprinting()
    {
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed
            || Gamepad.current != null && Gamepad.current.leftStickButton.isPressed;
    }

    static bool WasJumpPressed()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }
}
