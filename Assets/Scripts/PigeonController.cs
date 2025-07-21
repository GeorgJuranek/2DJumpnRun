using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PigeonController : MonoBehaviour
{
    Rigidbody2D pigeonRigidBody;
    Animator pigeonAnimator;
    SpriteRenderer pigeonSprite;

    RigidbodyConstraints2D originalConstraints;

    [SerializeField] int jumpForce;
    [SerializeField] int walkSpeed;
    [SerializeField] LayerMask Ground;
    [Tooltip("for test level recommended")]
    [SerializeField] bool useMultiJump;
    [Tooltip("for test level recommended with value: 4")]
    [SerializeField] int maxExtraJumps;
    [Tooltip("for test level recommended")]
    [SerializeField] bool useGliding;

    PigeonControl newInput;
    float movementX;

    bool isGrounded = false;
    bool canJump = false;

    int currentExtraJumps;

    [Tooltip("for test level not necessarily recommended (confusing if is true)")]
    [SerializeField] bool useCoyoteTime;

    [Tooltip("recommended with value: 0.05")]
    [SerializeField] float coyoteTimeInSeconds;

    float timeOfCoyoteInTheAir = 0.0f;
    bool isWithinCoyoteTime;

    [Tooltip("for test level recommended")]
    [SerializeField] bool useJumpLoad;

    [Tooltip("for test level recommended with value: 10;")]
    [SerializeField] float maxJumpLoad;
    float jumpLoad;

    [SerializeField] LayerMask Building;
    bool isAgainstWall;

    [SerializeField] LayerMask Deathzone;

    bool isInDeathzone;
    public bool IsInDeathzone
    {
        get => isInDeathzone;
        set
        {
            if (isInDeathzone == value) return;

            if (value)
            {
                pigeonRigidBody.constraints = RigidbodyConstraints2D.FreezePositionX;
            }
            else
            {
                pigeonRigidBody.constraints = originalConstraints;
            }

            isInDeathzone = value;
        }
    }

    Vector3 pigeonStartPosition;

    bool isAgainstGround;

    public delegate void JumpsChangedEventHandler(int jumps, bool canJump, bool coyoteTime, bool isDoomedToDeath);
    public static event JumpsChangedEventHandler OnJumpsChanged;
    
    public delegate void CongratulationChangedEventHandler(bool isDoomedToDeath);
    public static event CongratulationChangedEventHandler OnCongratulationChanged;

    bool firstGroundContactDidNotHappenYet;
    bool isDoomedToDeath;

    [SerializeField] float forceAgainstWallcliping;

    private AudioSource audioSource;
    public AudioClip pigeonCooCooSound;
    public AudioClip pigeonWingFlapSound;
    public AudioClip pigeonHitWallSound;

    ParticleSystem pigeonParticles;

    [SerializeField] LayerMask Nests;
    bool isAtNest;

    [SerializeField] Transform pigeonBottom;
    [SerializeField] Transform pigeonLeftSide;
    [SerializeField] Transform pigeonRightSide;

    void Awake()
    {
        PigeonInit();
    }

    void FixedUpdate()
    {
        Collisions();

        PigeonMovement();

        CheckIfRanIntoDeath();

        PigeonDeathdrive(isDoomedToDeath);

        HelpWithEdges();

        CheckForGroundReset();

        TryDoCoyoteJump();

        TryDoGliding();

        TryDoJumpLoad();

        TryDoMultiJump();
    }

    void Update()
    {
        PigeonAnimationCircuit();
    }

    private void PigeonInit()
    {
        pigeonStartPosition = transform.position;

        pigeonRigidBody = GetComponent<Rigidbody2D>();
        originalConstraints = pigeonRigidBody.constraints;

        pigeonAnimator = GetComponent<Animator>();
        pigeonSprite = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        pigeonParticles = GetComponentInChildren<ParticleSystem>();

        newInput = new PigeonControl();

        PlayCooCooSound();
        pigeonParticles.Play();
    }

    private void PigeonMovement()
    {
        movementX = newInput.pigeonActionMap.Movement.ReadValue<float>();
        pigeonRigidBody.velocity = new Vector2(movementX * walkSpeed, pigeonRigidBody.velocity.y);
    }

    private void OnEnable()
    {
        newInput.pigeonActionMap.Enable();
        newInput.pigeonActionMap.Jump.started += Jump;
        newInput.pigeonActionMap.LoadJump.canceled += LoadJump;
    }

    private void OnDisable()
    {
        newInput.pigeonActionMap.Disable();
        newInput.pigeonActionMap.Jump.started -= Jump;
        newInput.pigeonActionMap.LoadJump.canceled -= LoadJump;
    }

    private bool CheckCoyoteTime()
    {
        return timeOfCoyoteInTheAir <= coyoteTimeInSeconds;
    }

    private void PigeonAnimationCircuit()
    {
        pigeonAnimator.SetBool("isGrounded", isGrounded);

        if (useJumpLoad)
        {
            pigeonAnimator.SetFloat("loadingJump", 2 * (jumpLoad / maxJumpLoad));
        }

        if (useCoyoteTime)
        {
            pigeonAnimator.SetBool("isCoyote", isWithinCoyoteTime);
        }
        else
        {
            pigeonAnimator.SetBool("isCoyote", true);
        }

        if (movementX != 0f)
        {
            pigeonAnimator.SetBool("isWalking", movementX != 0f);

            if (movementX > 0)
            {
                pigeonSprite.flipX = false;
            }
            else
            {
                pigeonSprite.flipX = true;
            }
        }
        else
        {
            pigeonAnimator.SetBool("isWalking", false);
        }

        if (pigeonRigidBody.velocity.y > 0)
        {
            pigeonAnimator.SetBool("isJumping", true);
        }
        else
        {
            pigeonAnimator.SetBool("isJumping", false);
        }

        pigeonAnimator.SetBool("isDying", IsInDeathzone);
    }

    #region PigeonCollisions
        private void Collisions()
        {
            isGrounded = Physics2D.OverlapBox(pigeonBottom.position, transform.GetChild(0).localScale, 0, Ground) != null;
            isAgainstWall = ((Physics2D.OverlapBox(pigeonLeftSide.position, pigeonLeftSide.localScale, 0, Building) != null) || (Physics2D.OverlapBox(pigeonRightSide.position, pigeonRightSide.localScale, 0, Building) != null)) && !isGrounded;
            isAgainstGround = (Physics2D.OverlapBox(pigeonLeftSide.position, pigeonLeftSide.localScale, 0, Ground) != null) || (Physics2D.OverlapBox(pigeonRightSide.position, pigeonRightSide.localScale, 0, Ground) != null);
            IsInDeathzone = Physics2D.OverlapBox(pigeonBottom.position, pigeonBottom.localScale, 0, Deathzone) != null;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject.tag == "Nest")
            {
                isAtNest = true;
                UpdateCongratulationGUI(isAtNest);
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.gameObject.tag == "Nest")
            {
                isAtNest = false;
                UpdateCongratulationGUI(isAtNest);
            }
        }

        private void HelpWithEdges()
        {
            if (!isGrounded && isAgainstGround) //Edge case
            {
                pigeonRigidBody?.AddForce(new Vector3(-1, forceAgainstWallcliping, 0), ForceMode2D.Impulse);
            }
        }
    #endregion

    #region TryPigeonAbilities
        private void TryDoGliding()
        {
            if (!useGliding) return;

            Gliding();
        }

        private void TryDoMultiJump()
        {
            if (useMultiJump) return;

            currentExtraJumps = 0;
            maxExtraJumps = 0;

        }

        private void TryDoJumpLoad()
        {
            if (!useJumpLoad) return;

            if (newInput.pigeonActionMap.LoadJump.inProgress && canJump)
            {
                if (jumpLoad < maxJumpLoad)
                {
                    jumpLoad += 0.1f;
                }
            }
            else
            {
                jumpLoad = 0f;
            }

            if (useCoyoteTime && !isWithinCoyoteTime)//resets jumpLoad when coyoteTime is expired
            {
                jumpLoad = 0f;
            }
        }

        private void TryDoCoyoteJump()
        {
            if (!useCoyoteTime) return;

            if (!isGrounded && canJump)
            {
                timeOfCoyoteInTheAir += 0.001f;
                isWithinCoyoteTime = CheckCoyoteTime();

                if (!isWithinCoyoteTime)
                {
                    UpdateGUI(currentExtraJumps, canJump, isWithinCoyoteTime : false); //optionale parameter!
                }
            }
        }
    #endregion

    #region DoPigeonAbilities
        private void Jump(InputAction.CallbackContext obj)
        {
            if ((useCoyoteTime ? isWithinCoyoteTime : true) && !isDoomedToDeath && !IsInDeathzone && pigeonRigidBody.velocity.y < jumpForce / 3)
            {
                if (canJump)
                {
                    pigeonRigidBody?.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    canJump = false;
                }
                else if (useMultiJump && currentExtraJumps > 0)
                {
                    pigeonRigidBody?.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    currentExtraJumps--;
                }

                UpdateGUI(currentExtraJumps, canJump);
            }

            jumpLoad = 0f;
        }

        private void LoadJump(InputAction.CallbackContext obj)
        {
            if (useCoyoteTime ? isWithinCoyoteTime : true)
            {
                if (useJumpLoad && jumpLoad > 1 && canJump)
                {
                    pigeonRigidBody?.AddForce(Vector2.up * jumpLoad, ForceMode2D.Impulse);
                    canJump = false;
                    UpdateGUI(currentExtraJumps, canJump);
                }
            }

            jumpLoad = 0f;
        }

        private void Gliding()
        {
            if (!isGrounded && pigeonRigidBody.velocity.y < 0)
            {
                pigeonRigidBody.gravityScale /= 1.2f;

                if (pigeonSprite.flipX)
                {
                    pigeonRigidBody.velocity = new Vector2(-1 * walkSpeed, pigeonRigidBody.velocity.y);
                }
                else
                {
                    pigeonRigidBody.velocity = new Vector2(walkSpeed, pigeonRigidBody.velocity.y);
                }

                jumpLoad = 0f;
            }
            else
            {
                pigeonRigidBody.gravityScale = 1;
            }
        }
    #endregion

    #region TheLifeAndDeathOfAnOrdinaryPigeon
        private void CheckIfRanIntoDeath()
        {
            if (isAgainstWall && !isAgainstGround && !isGrounded)
            {
                if (!isDoomedToDeath)
                {
                    PlayHitWallSound();
                    newInput.pigeonActionMap.Disable();
                    isDoomedToDeath = true;
                    UpdateGUI(currentExtraJumps, canJump, isDoomedToDeath : true);
                }
            }
        }

        private void PigeonDeathdrive(bool isDoomedToDeath)
        {
            if (isDoomedToDeath)
            {
                pigeonSprite.flipY = true;
                pigeonRigidBody.gravityScale = 10;
                UpdateGUI(currentExtraJumps, canJump, isDoomedToDeath : true);

                if (isInDeathzone)
                {
                    pigeonSprite.flipY = false;
                }
            }
        }

        private void PigeonReset()
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = pigeonCooCooSound;
                audioSource.Play();
            }

            isDoomedToDeath = false;
            pigeonSprite.flipY = false;
            pigeonRigidBody.gravityScale = 1;
            timeOfCoyoteInTheAir = 0.0f;
            isWithinCoyoteTime = CheckCoyoteTime();
            canJump = true;
            newInput.pigeonActionMap.Enable();

            if (useMultiJump && currentExtraJumps < maxExtraJumps)
            {
                currentExtraJumps = maxExtraJumps;
            }

            UpdateGUI(currentExtraJumps, canJump, isWithinCoyoteTime, isDoomedToDeath);
        }

        private void CheckForGroundReset()
        {
            if (isDoomedToDeath && isGrounded)
            {
                PigeonReset();
            }
            else
            {
                if (isGrounded)
                {
                    if (firstGroundContactDidNotHappenYet && pigeonRigidBody.velocity.y == 0) //this is used to detect the entry of ground collision and fired only one time
                    {
                        PigeonReset();
                        firstGroundContactDidNotHappenYet = false;
                    }
                }
                else
                {
                    firstGroundContactDidNotHappenYet = true;
                }
            }
        }

        private void PigeonRebirth() //used by AnimationEvent "isDying"
        {
            PigeonReset();
            pigeonSprite.flipX = false;
            pigeonRigidBody.velocity = new Vector2(0, 0);
            transform.position = pigeonStartPosition;
            pigeonParticles.Play();
        }
    #endregion

    #region UsedByDelegates
        public void UpdateGUI(int jumps, bool canJump, bool isWithinCoyoteTime = true, bool isDoomedToDeath = false) // used by GUIManager script
        {
            OnJumpsChanged?.Invoke(jumps, canJump, isWithinCoyoteTime, isDoomedToDeath);
        }

        public void UpdateCongratulationGUI(bool isAtNest)
        {
            OnCongratulationChanged?.Invoke(isAtNest);
        }
    #endregion

    #region AudioClips
    public void PlayCooCooSound()
    {
        if (!audioSource.isPlaying && DateTime.Now.Second % 2 == 1)
        {
            audioSource.PlayOneShot(pigeonCooCooSound);
        }

    }

    public void PlayWingFlapSound()
    {
        audioSource.clip = pigeonWingFlapSound;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

    }

    public void PlayHitWallSound()
    {
        audioSource.clip = pigeonHitWallSound;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

    }
    #endregion

}

