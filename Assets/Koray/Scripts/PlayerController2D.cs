using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;   // Saldırı tekrarına izin veren süre
    public float attackDuration = 0.2f;   // Saldırı animasyon/işlem süresi
    public Transform attackPoint;         // Kılıcın ucu/kılıca yakın bir nokta
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public int attackDamage = 10;

    [Header("Wall Climb Settings")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public float wallSlideSpeed = 2f;
    public float wallClimbSpeed = 3f;
    public float wallJumpForce = 7f;
    public LayerMask wallLayer;

    [Header("Dash Settings")]
    [Tooltip("Dash hızı (x ekseninde). Karakter ne kadar güçlü atılsın?")]
    public float dashSpeed = 10f;

    [Tooltip("Dash süresi. (Karakterin hızlı şekilde ilerleyeceği zaman aralığı)")]
    public float dashDuration = 0.2f;

    [Tooltip("Dash tekrar kullanılmadan önce beklenmesi gereken süre.")]
    public float dashCooldown = 1f;

    [Tooltip("Dash için kullanılacak tuş. Örn: E.")]
    public KeyCode dashKey = KeyCode.E;

    // Duvardan geri sekme (Wall Bounce) ayarları
    [Header("Wall Bounce Settings")]
    [Tooltip("Wall Bounce için kullanılacak tuş (örneğin R).")]
    public KeyCode wallBounceKey = KeyCode.Space;
    [Tooltip("Duvardan sekme sırasında x ekseninde uygulanacak kuvvet.")]
    public float wallBounceHorizontalForce = 5f;
    [Tooltip("Duvardan sekme sırasında y ekseninde uygulanacak kuvvet.")]
    public float wallBounceVerticalForce = 5f;
    [Tooltip("Wall Bounce süresi.")]
    public float wallBounceDuration = 0.2f;
    [Tooltip("Wall Bounce yapabilmek için bekleme süresi.")]
    public float wallBounceCooldown = 1f;

    [Header("Wall Bounce Facing Cooldown")]
    public float wallBounceFacingCooldown = 0.5f;
    private float wallBounceFacingTimer = 0f;
    
    [Header("Bullet Throw Settings")]
    public float bulletThrowForce = 10f;

    [Header("Combo Settings")]
    public float comboTimeout = 1f; // Komboyu devam ettirmek için max bekleme süresi

    private float comboTimer = 0f;
    private bool inCombo = false;
    private int lastAttackType = 0;


    private Rigidbody2D _rb;
    private Animator _anim;

    // Hareket ve durum değişkenleri
    private float _horizontalInput;
    private bool _isRunning;
    private bool _isGrounded;
    private bool _isAttacking;
    private float _attackTimer;

    // Duvarla ilgili durumlar
    private bool _isTouchingWall;
    private bool _isWallSliding;

    // Dash ile ilgili durum değişkenleri
    private bool _isDashing;
    private float _dashTimeLeft;      // Kalan dash süresi
    private float _lastDashTime;      // Son dash yapılan zaman

    // Wall Bounce durum değişkenleri
    private bool _isWallBouncing;
    private float _wallBounceTimeLeft;
    private float _lastWallBounceTime;

    public ParticleSystem Ziplama;
    public ParticleSystem Dusme;
    public ParticleSystem Isilti;

    public GameManagerScript gameManager;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Yatay girdi (A/D veya Sol/Sağ ok tuşları)
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        // Koşma tuşu
        _isRunning = Input.GetKey(KeyCode.LeftShift);

        // Zıplama
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_isWallSliding && !_isGrounded)
            {
                //WallJump();
            }
            else if (_isGrounded && !_isAttacking)
            {
                Jump();
            }
        }

        // Saldırı
        // Kombo saldırı denemesi
        if (Input.GetKeyDown(KeyCode.F) && _attackTimer <= 0f)
        {
            AttemptComboAttack();
        }

        // Kombo süresi takibi
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                // Süre bitti, kombo sıfırlanır
                inCombo = false;
                lastAttackType = 0;
            }
        }


        // Attack cooldown’u zamanla azalt
        if (_attackTimer > 0f)
        {
            _attackTimer -= Time.deltaTime;
        }

        // Dash giriş (E tuşu)
        if (Input.GetKeyDown(dashKey))
        {
            // Cooldown geçmiş mi, şu an dash yapmıyor muyuz?
            if (!_isDashing && Time.time >= _lastDashTime + dashCooldown)
            {
                StartDash();
            }
        }

        // Eğer dash halindeysek kalan süreyi düşürüyoruz
        if (_isDashing)
        {
            _dashTimeLeft -= Time.deltaTime;
            if (_dashTimeLeft <= 0f)
            {
                StopDash();
            }
        }

        // DUVARDAN GERİ SEKME (Wall Bounce) girişleri
        // Sadece duvara değiyorken (wallCheck) wall bounce yapılabilir
        if (Input.GetKeyDown(wallBounceKey) && (_isTouchingWall || wallBounceFacingTimer > 0f))
        {
            if (!_isWallBouncing && Time.time >= _lastWallBounceTime + wallBounceCooldown)
                if(_isTouchingWall)
                {
                    StartWallBounce();
                }else
                {
                    StartWallBounce(-1);
                }
        }

        if (_isWallBouncing)
        {
            _wallBounceTimeLeft -= Time.deltaTime;
            if (_wallBounceTimeLeft <= 0f)
                StopWallBounce();
        }

        if (wallBounceFacingTimer > 0f)
        {
            wallBounceFacingTimer -= Time.deltaTime;
        }

        UpdateAnimator();
    }

    void FixedUpdate()
    {
        // Zeminde miyiz kontrolü
        CheckGround(_isGrounded);

        // Duvar kontrolü
        CheckWall();

        // Eğer dash, wall bounce veya saldırı durumunda normal hareket iptal edilsin
        if (_isDashing || _isWallBouncing || _isAttacking)
            return;

        // Duvardaysak duvar tırmanma/slide hareketi
        if (_isWallSliding && !_isGrounded)
        {
            WallSlideMovement();
        }
        else
        {
            // Normal yatay hareket
            float currentSpeed = _isRunning ? runSpeed : walkSpeed;
            Vector2 velocity = _rb.linearVelocity;
            velocity.x = _horizontalInput * currentSpeed;
            _rb.linearVelocity = velocity;
        }

        // Yön değiştirme (Sprite flip)
        if (_horizontalInput > 0 && transform.localScale.x < 0)
        {
            Flip();
        }
        else if (_horizontalInput < 0 && transform.localScale.x > 0)
        {
            Flip();
        }
    }

    // =====================================================
    // =============== Duvar Tırmanma Fonksiyonları ========
    // =====================================================
    private void CheckWall()
    {
        float direction = transform.localScale.x;
        Vector2 checkPos = wallCheck.position;
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.right * direction, wallCheckDistance, wallLayer);

        _isTouchingWall = (!_isGrounded && hit.collider != null);
        _isWallSliding = _isTouchingWall;
    }

    private void WallSlideMovement()
    {
        Vector2 velocity = _rb.linearVelocity;

        if(_horizontalInput == 0)
        {
            if (velocity.y < -wallSlideSpeed)
            {
                velocity.y = -wallSlideSpeed;
            }
        }
        _rb.linearVelocity = velocity;
    }
    // --------------------- Wall Bounce Fonksiyonları ---------------------

    private void StartWallBounce(int direction = 1)
    {
        
        _isWallBouncing = true;
        _wallBounceTimeLeft = wallBounceDuration;
        _lastWallBounceTime = Time.time;
        // Karakter duvara değiyorsa, bakış yönünün tersine doğru wall bounce yaparız
        float bounceDirection = transform.localScale.x * -1f;
        _rb.linearVelocity = new Vector2(bounceDirection * direction * wallBounceHorizontalForce, wallBounceVerticalForce);
        if (_anim) _anim.SetTrigger("WallBounce");
    }

    private void StopWallBounce()
    {
        _isWallBouncing = false;
        Vector2 currentVel = _rb.linearVelocity;
        currentVel.x = 0f;
        _rb.linearVelocity = currentVel;
    }

    // ---------------------------------------------------------------------

    // =====================================================
    // =============== Dash Fonksiyonları ==================
    // =====================================================
    private void StartDash()
    {
        _isDashing = true;
        _dashTimeLeft = dashDuration;
        _lastDashTime = Time.time;

        // Karakterin baktığı yönü alalım
        float dashDirection = Mathf.Sign(transform.localScale.x);

        // Karakteri yatay eksende hızla fırlat
        _rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

        // Opsiyonel: Animasyon tetiklemek isterseniz
        if (_anim)
        {
            _anim.SetTrigger("Dash");
        }
    }

    private void StopDash()
    {
        _isDashing = false;
        // Dash bitince, isterseniz horizontal velocity'yi sıfırlayabilirsiniz
        // ya da mevcut _rb.linearVelocity.y değerini koruyarak x'i 0 yapabilirsiniz.
        Vector2 currentVel = _rb.linearVelocity;
        currentVel.x = 0f;
        _rb.linearVelocity = currentVel;
    }

    // =====================================================
    // =============== Normal Zıplama & Zemin ==============
    // =====================================================
    private void Jump()
    {
        Vector2 velocity = _rb.linearVelocity;
        velocity.y = jumpForce;
        _rb.linearVelocity = velocity;
        Ziplama.Play();

        if (_anim)
            _anim.SetTrigger("JumpUp"); // Yukarı zıplama animasyonu

    }

    private void CheckGround(bool eskiisGrounded)
    {
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);


        _isGrounded = (hit != null);
        if (_isGrounded && !eskiisGrounded) 
        {
            Dusme.Play();
        }
    }

    private void Flip()
    {
        if (_isTouchingWall)
        {
            wallBounceFacingTimer = wallBounceFacingCooldown;
        }
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // =====================================================
    // ==================== Saldırı ========================
    // =====================================================

    private void AttemptComboAttack()
    {
        // Eğer komboda değilsek veya süre dolduysa ilk saldırı (1) ile başla
        if (!inCombo || comboTimer <= 0f)
        {
            lastAttackType = 1;
            inCombo = true;
            StartAttack(1);
        }
        else
        {
            // Önceki saldırıyla aynı olmayan rastgele bir saldırı tipi seç (1,2,3)
            int nextAttack = lastAttackType;
            while (nextAttack == lastAttackType)
            {
                nextAttack = Random.Range(1, 4); // 1, 2 veya 3
            }
            lastAttackType = nextAttack;
            StartAttack(nextAttack);
        }

        // Kombo süresini yenile
        comboTimer = comboTimeout;
    }

    private void StartAttack(int attackType)
    {
        _isAttacking = true;
        _attackTimer = attackCooldown;

        // Hangi saldırı tipiyse, Animator'da ilgili trigger'ı tetikle
        if (_anim)
        {
            // Aynı karede tetiklenme karışmasın diye önce resetliyoruz
            _anim.ResetTrigger("Attack1");
            _anim.ResetTrigger("Attack2");
            _anim.ResetTrigger("Attack3");

            if (attackType == 1) _anim.SetTrigger("Attack1");
            else if (attackType == 2) _anim.SetTrigger("Attack2");
            else if (attackType == 3) _anim.SetTrigger("Attack3");
        }

        // Mevcut saldırı mantığı (hasar verme vb.)
        PerformAttack();

        // Saldırı animasyon süresi bitince ResetAttack çağrılıyor
        Invoke(nameof(ResetAttack), attackDuration);
    }


    private void PerformAttack()
    {
        // Attack alanındaki tüm objeleri alıyoruz (layer filtresi uygulamıyoruz ki hem enemy hem de bullet kontrol edilebilsin)
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);
        foreach (Collider2D obj in hitObjects)
        {
            // Eğer objenin layer'ı "bullet" ise:
            if (obj.gameObject.layer == LayerMask.NameToLayer("Shuriken"))
            {
                Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
                    Instantiate(Isilti, obj.transform.position, Quaternion.identity);
                    // Karakterin facing yönünü al (örneğin, sağa bakıyorsa +1, sola -1)
                    float facing = Mathf.Sign(transform.localScale.x);
                    // x bileşeni kesinlikle karakterin tersine, y bileşeni hafif rastgele (örnek: -0.5 ile 0.5 arası)
                    Vector2 throwDirection = new Vector2(facing, Random.Range(-0.5f, 0.5f)).normalized;
                    rb.AddForce(throwDirection * bulletThrowForce, ForceMode2D.Impulse);
                }
            }
            else
            {
                // Diğer objeler için, örneğin enemy varsa hasar verelim:
                NPCBase enemyScript = obj.GetComponent<NPCBase>();
                if (enemyScript != null)
                {
                    enemyScript.GetDamage();
                }
            }
        }
    }


    private void ResetAttack()
    {
        _isAttacking = false;
    }

    // =====================================================
    // =================== Animasyon =======================
    // =====================================================
    private void UpdateAnimator()
    {
        if (_anim == null)
            return;

        _anim.SetFloat("Speed", Mathf.Abs(_rb.linearVelocity.x));
        _anim.SetFloat("YAxisSpeed", Mathf.Abs(_rb.linearVelocity.y));
        _anim.SetBool("IsGrounded", _isGrounded);
        _anim.SetBool("IsRunning", _isRunning);
        _anim.SetBool("IsWallSlidingDown", _isWallSliding);
        _anim.SetBool("IsDashing", _isDashing);
        _anim.SetBool("IsFalling", !_isGrounded && _rb.linearVelocity.y < 0);
    }


    // =====================================================
    // =================== Gizmos Debug ====================
    // =====================================================
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            float direction = (transform != null) ? transform.localScale.x : 1f;
            Vector2 startPos = wallCheck.position;
            Vector2 endPos = startPos + Vector2.right * direction * wallCheckDistance;
            Gizmos.DrawLine(startPos, endPos);
        }
    }

    public void GetHit()
    {
        //ölme ile ilgili animasyonlar

        gameManager.SeviyeTekrari();
    }

}
