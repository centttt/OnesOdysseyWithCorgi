using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 23;
    [Space(5)]

    [Header("Vertical Movement Settings")]
    [SerializeField] private float jumpForce = 15;
    private float jumpBufferCounter = 0;
    [SerializeField] private float jumpBufferFrames;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;
    [Space(5)]

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dashEffect;
    [Space(5)]

    [Header("Attack Settings")]
    [SerializeField] Transform SideAttackTransform;
    [SerializeField] Vector2 SideAttackArea;

    [SerializeField] Transform UpAttackTransform;
    [SerializeField] Vector2 UpAttackArea;

    [SerializeField] Transform DownAttackTransform;
    [SerializeField] Vector2 DownAttackArea;

    [SerializeField] LayerMask attackableLayer;
    [SerializeField] float damage;

    [SerializeField] GameObject slashEffect;
    bool attack = false;
    float timeBetweenAttack, timeSinceAttack;
    [Space(5)]

    [Header("Recoil")]
    [SerializeField] int recoilXsteps = 10;
    [SerializeField] int recoilYsteps = 5;
    [SerializeField] float recoilXspeed = 25;
    [SerializeField] float recoilYspeed = 20;
    int stepsYRecoiled, stepsXRecoiled;

    [Header("Health Settings")]
    public int health;
    public int maxHealth;
    public SpriteRenderer playerSr;

    [HideInInspector] public PlayerStateList pState;
    private Rigidbody2D rb;
    private float xAxis, yAxis;
    private float gravity;
    Animator anim;
    private bool canDash;
    private bool dashed;

    public static PlayerController Instance;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        health = maxHealth;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        pState = GetComponent<PlayerStateList>();
        gravity = rb.gravityScale;
        canDash = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    void Update()
    {
        GetInputs();
        UpdateJumpVariable();
        Flip();
        if (pState.dashing) return;
        Move();
        Jump();
        StartDash();
        Attack();
        Recoil();
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        attack = Input.GetMouseButtonDown(0);
    }

    void Flip() // if player clicks the left button, character faces left, otherwise, right
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
            pState.lookingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
            pState.lookingRight = true;
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(walkSpeed * xAxis, rb.linearVelocity.y);
        anim.SetBool("Walking", rb.linearVelocity.x != 0 && Grounded());
    }

    void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (Grounded())
        {
            dashed = false;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");
            if (yAxis == 0 || yAxis < 0 && Grounded())
            {
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, recoilXspeed);
                Destroy(Instantiate(slashEffect, SideAttackTransform), 0.1f);
            }
            else if (yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, recoilYspeed);
                SlashEffectAngle(slashEffect, 80, UpAttackTransform);
            }
            else if (yAxis < 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, recoilYspeed);
                SlashEffectAngle(slashEffect, -80, DownAttackTransform);
            }
        }
    }

    void SlashEffectAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        Destroy(_slashEffect, 0.1f);
    }

    void Recoil()
    {
        if (pState.recoilingX)
        {
            if (pState.lookingRight)
            {
                rb.linearVelocity = new Vector2(-recoilXspeed, 0);
            }
            else
            {
                rb.linearVelocity = new Vector2(recoilXspeed, 0);
            }
        }

        if (pState.recoilingY)
        {
            rb.gravityScale = 0;
            if (yAxis < 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, recoilYspeed);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -recoilYspeed);
            }
            airJumpCounter = 0;
        }
        else
        {
            rb.gravityScale = gravity;
        }

        // stop recoiling 
        if (pState.recoilingX && stepsXRecoiled < recoilXsteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }

        if (pState.recoilingY && stepsYRecoiled < recoilYsteps)
        {
            stepsYRecoiled++;
        }
        else
        {
            StopRecoilY();
        }

        if (Grounded())
        {
            StopRecoilY();
        }
    }

    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }

    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    }

    public void TakeDamage(float _damage)
    {
        health -= Mathf.RoundToInt(_damage);
        if (health <= 0)
        {
            rb.linearVelocity = Vector2.zero;

            if(playerSr != null)
            {
                playerSr.enabled = false;
            }

            enabled = false;

        }
        else
        {
            // 3. Only run the invincibility/hurt animation if the player SURVIVED
            StartCoroutine(StopTakingDamage());
        }
    }

    IEnumerator StopTakingDamage()
    {
        pState.invincible = true;
        anim.SetTrigger("TakeDamage");
        ClampHealth();
        yield return new WaitForSeconds(1f);
        pState.invincible = false;
    }

    void ClampHealth()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);

        // Creating a list to remember who we already hit this frame
        List<Enemy> enemiesHitThisFrame = new List<Enemy>();

        if (objectsToHit.Length > 0)
        {
            _recoilDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++)
        {
            Enemy enemyComponent = objectsToHit[i].GetComponent<Enemy>();

            // Check if this object actually has an enemy script AND if we haven't hit it yet
            if (enemyComponent != null && !enemiesHitThisFrame.Contains(enemyComponent))
            {
                enemyComponent.EnemyHit(damage, (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);
                // Add to the "Already Hit" list so we don't hit it again
                enemiesHitThisFrame.Add(enemyComponent);
            }
        }
    }

    private bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Jump()
    {
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            pState.jumping = false;
        }

        if (!pState.jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                pState.jumping = true;
            }
            else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
            {
                pState.jumping = true;
                airJumpCounter++;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        anim.SetBool("Jumping", !Grounded());
    }

    void UpdateJumpVariable()
    {
        if (Grounded())
        {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter = jumpBufferCounter - Time.deltaTime * 10;
        }
    }
}