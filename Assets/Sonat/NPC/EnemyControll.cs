using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float patrolSpeed = 2f;       // Devriye hýzý
    public float chaseSpeed = 3f;        // Kovalama hýzý
    public float jumpForce = 5f;         // Zýplama kuvveti

    [Header("Algýlama ve Saldýrý")]
    public float detectionRange = 5f;    // Ýleriye yönelik algýlama mesafesi
    public float attackRange = 1f;       // Saldýrý mesafesi
    public float attackCooldown = 1f;    // Saldýrý sonrasý bekleme süresi (1 saniye)
    private float attackTimer = 0f;
    public float detectionRayOffset = 0.5f;  // Raycast için dikey ofset

    [Header("Algýlama Layer Mask")]
    public LayerMask detectionLayerMask; // NPC'nin görmezden gelmesini istediðiniz layer'larý belirleyin

    [Header("Chase Bellek Süresi")]
    public float chaseMemoryTime = 2f;   // Oyuncu tespitini kaybederse kovalamanýn devam edeceði süre
    private float chaseTimer = 0f;
    private int chaseDirectionSign = 0;  // Kovalamaya baþlandýðý andaki yön (1 = sað, -1 = sol)

    [Header("Devriye Noktalarý")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Yer Kontrolü")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    private Transform player;
    private Rigidbody2D rb;

    private enum NPCState { Patrol, Chase }
    private NPCState state = NPCState.Patrol;

    // NPC’nin anlýk bakýþ yönü ve son bilinen yönü (patrol esnasýnda korunur)
    private Vector2 facingDirection = Vector2.right;
    private Vector2 lastFacingDirection = Vector2.right;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null)
            return;

        // Yer kontrolü
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        switch (state)
        {
            case NPCState.Patrol:
                Patrol();
                // Ýleriye yönelik 2 raycast ile oyuncu algýlama
                if (IsPlayerDetected())
                {
                    state = NPCState.Chase;
                    chaseTimer = chaseMemoryTime;
                    chaseDirectionSign = (player.position.x - transform.position.x) >= 0 ? 1 : -1;
                }
                break;

            case NPCState.Chase:
                ChaseAndAttack();
                break;
        }
    }

    // Devriye modunda belirlenen noktalar arasýnda hareket
    void Patrol()
    {
        if (patrolPoints.Length == 0)
            return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];

        if (Vector2.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            // Hedefe doðru giderken bakýþ yönünü güncelle
            facingDirection = (targetPoint.position.x - transform.position.x) >= 0 ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;
        }
        else
        {
            // Noktaya ulaþýldýðýnda son yön korunur ve sonraki patrol noktasýna geçilir
            facingDirection = lastFacingDirection;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, patrolSpeed * Time.deltaTime);
    }

    // Ýleriye yönelik 2 raycast (üst ve alt) kullanarak oyuncu algýlamasý
    bool IsPlayerDetected()
    {
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        RaycastHit2D hitUpper = Physics2D.Raycast(originUpper, facingDirection, detectionRange, detectionLayerMask);
        RaycastHit2D hitLower = Physics2D.Raycast(originLower, facingDirection, detectionRange, detectionLayerMask);

        if ((hitUpper.collider != null && hitUpper.collider.CompareTag("Player")) ||
            (hitLower.collider != null && hitLower.collider.CompareTag("Player")))
        {
            return true;
        }
        return false;
    }

    // Kovalama ve saldýrý durumu
    void ChaseAndAttack()
    {
        float deltaX = player.position.x - transform.position.x;
        bool detected = IsPlayerDetected();

        if (detected)
        {
            // Eðer oyuncu NPC’nin baktýðý yönde ise, chase belleðini sýfýrla ve yön güncelle
            chaseTimer = chaseMemoryTime;
            chaseDirectionSign = (deltaX >= 0) ? 1 : -1;

            // Oyuncuya doðru hareket et (sadece yatay)
            Vector2 newPos = transform.position;
            newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
            transform.position = newPos;

            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;
        }
        else
        {
            // Eðer oyuncu ileriye dönük raycast’te tespit edilemiyorsa, bellekte kalan süre boyunca
            // önceki yön bilgisiyle (chaseDirectionSign) hareket etmeye devam et.
            chaseTimer -= Time.deltaTime;
            if (chaseTimer > 0f)
            {
                Vector2 newPos = transform.position;
                newPos.x += chaseSpeed * Time.deltaTime * chaseDirectionSign;
                transform.position = newPos;
            }
        }

        // Yükseklik farký varsa ve NPC yerdeyse zýplama
        float verticalDifference = player.position.y - transform.position.y;
        if (verticalDifference > 0.5f && isGrounded)
        {
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }

        // Saldýrý: Oyuncu attackRange içindeyse saldýr, ardýndan attackCooldown süresi bekle
        if (Mathf.Abs(deltaX) <= attackRange)
        {
            if (attackTimer <= 0f)
            {
                AttackPlayer();
                attackTimer = attackCooldown;
            }
            else
            {
                attackTimer -= Time.deltaTime;
            }
        }
        else
        {
            attackTimer = 0f;
        }

        // Eðer chase belleði süresi dolduysa, devriye moduna dön
        if (chaseTimer <= 0f)
        {
            state = NPCState.Patrol;
        }
    }

    // Saldýrý iþlemi (örneðin: hasar verme, animasyon oynatma vb.)
    void AttackPlayer()
    {
        Debug.Log("Player'a saldýrýldý!");
    }

    // Seçildiðinde, NPC’nin baktýðý yönde 2 raycast (üst ve alt) gösteren Gizmo çizimi
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        Gizmos.DrawLine(originUpper, originUpper + facingDirection * detectionRange);
        Gizmos.DrawLine(originLower, originLower + facingDirection * detectionRange);
    }
}
