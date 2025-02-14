using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float patrolSpeed = 2f;       // Devriye hýzý
    public float chaseSpeed = 3f;        // Kovalama hýzý

    [Header("Algýlama ve Saldýrý")]
    public float detectionRange = 5f;    // Ýleriye yönelik algýlama mesafesi
    public float attackRange = 1f;       // Saldýrý mesafesi
    public float attackCooldown = 1f;    // Saldýrý sonrasý bekleme süresi (1 saniye)
    private float attackTimer = 0f;
    public float detectionRayOffset = 0.5f;  // Raycast için dikey ofset
    public LayerMask detectionLayerMask;     // NPC’nin kendi collider’ýný ya da istenmeyen layer’larý görmezden gelmek için

    [Header("Chase Bellek Süresi")]
    public float chaseMemoryTime = 2f;   // Oyuncu belirli süre algýlanmazsa devriyeye dönme süresi
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

    // NPC’nin bakýþ yönü bilgisi (raycast ve hareket için)
    private Vector2 facingDirection = Vector2.right;
    private Vector2 lastFacingDirection = Vector2.right;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (player == null)
            return;

        // Yer kontrolü
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

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

    // Devriye modunda belirlenmiþ noktalar arasýnda hareket
    void Patrol()
    {
        if (patrolPoints.Length == 0)
            return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (Vector2.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            facingDirection = (targetPoint.position.x - transform.position.x) >= 0 ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;
            transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, patrolSpeed * Time.deltaTime);
        }
        else
        {
            facingDirection = lastFacingDirection;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    // Ýleriye yönelik üst ve alt offset’li 2 raycast ile oyuncu algýlama
    bool IsPlayerDetected()
    {
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        RaycastHit2D hitUpper = Physics2D.Raycast(originUpper, facingDirection, detectionRange, detectionLayerMask);
        RaycastHit2D hitLower = Physics2D.Raycast(originLower, facingDirection, detectionRange, detectionLayerMask);
        return (hitUpper.collider != null && hitUpper.collider.CompareTag("Player")) ||
               (hitLower.collider != null && hitLower.collider.CompareTag("Player"));
    }

    // Chase modunda, oyuncu eðer yukarýdaysa NPC zýplamýyor; yalnýzca x eksenindeki hizaya gelmeye çalýþýyor
    void ChaseAndAttack()
    {
        // NPC ile oyuncu arasýndaki yatay mesafeyi hesapla
        float deltaX = player.position.x - transform.position.x;

        // Oyuncunun tespit edilip edilmediðini kontrol et
        bool detected = IsPlayerDetected();

        if (detected)
        {
            // Chase zamanlayýcýsýný sýfýrla
            chaseTimer = chaseMemoryTime;

            // NPC'nin bakýþ yönünü belirle
            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;

            // Eðer oyuncu saldýrý menzilinin dýþýndaysa, NPC'yi oyuncunun x pozisyonuna doðru hareket ettir
            if (Mathf.Abs(deltaX) > attackRange)
            {
                // Hedef x pozisyonunu hesapla (saldýrý menzilinin sýnýrýnda duracak þekilde)
                float targetX = player.position.x - Mathf.Sign(deltaX) * attackRange;
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, targetX, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else
            {
                // Saldýrý menzilindeyse, saldýrý zamanlayýcýsýný kontrol et
                if (attackTimer <= 0f)
                {
                    // Saldýrýyý gerçekleþtir
                    AttackPlayer();
                    // Saldýrý sonrasý bekleme süresini ayarla
                    attackTimer = attackCooldown;
                }
                else
                {
                    // Saldýrý zamanlayýcýsýný azalt
                    attackTimer -= Time.deltaTime;
                }
            }
        }
        else
        {
            // Chase zamanlayýcýsýný azalt
            chaseTimer -= Time.deltaTime;

            // Chase zamanlayýcýsý sýfýrdan büyükse, NPC'yi oyuncunun son bilinen x pozisyonuna doðru hareket ettir
            if (chaseTimer > 0f)
            {
                // Hedef x pozisyonunu hesapla (saldýrý menzilinin sýnýrýnda duracak þekilde)
                float targetX = player.position.x - Mathf.Sign(deltaX) * attackRange;
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, targetX, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else
            {
                // Chase zamanlayýcýsý sýfýra ulaþtýysa, devriye moduna geri dön
                state = NPCState.Patrol;
            }
        }
    }





    void AttackPlayer()
    {
        Debug.Log("Player'a saldýrýldý!");
        // Buraya oyuncuya hasar verme veya animasyon tetikleme kodlarýný ekleyebilirsiniz.
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        Gizmos.DrawLine(originUpper, originUpper + facingDirection * detectionRange);
        Gizmos.DrawLine(originLower, originLower + facingDirection * detectionRange);
    }
}

