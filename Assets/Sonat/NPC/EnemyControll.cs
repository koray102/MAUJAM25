using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Hareket Ayarlar�")]
    public float patrolSpeed = 2f;       // Devriye h�z�
    public float chaseSpeed = 3f;        // Kovalama h�z�
    public float jumpForce = 5f;         // Z�plama kuvveti

    [Header("Alg�lama ve Sald�r�")]
    public float detectionRange = 5f;    // �leriye y�nelik alg�lama mesafesi
    public float attackRange = 1f;       // Sald�r� mesafesi
    public float attackCooldown = 1f;    // Sald�r� sonras� bekleme s�resi (1 saniye)
    private float attackTimer = 0f;
    public float detectionRayOffset = 0.5f;  // Raycast i�in dikey ofset

    [Header("Alg�lama Layer Mask")]
    public LayerMask detectionLayerMask; // NPC'nin g�rmezden gelmesini istedi�iniz layer'lar� belirleyin

    [Header("Chase Bellek S�resi")]
    public float chaseMemoryTime = 2f;   // Oyuncu tespitini kaybederse kovalaman�n devam edece�i s�re
    private float chaseTimer = 0f;
    private int chaseDirectionSign = 0;  // Kovalamaya ba�land��� andaki y�n (1 = sa�, -1 = sol)

    [Header("Devriye Noktalar�")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Yer Kontrol�")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    private Transform player;
    private Rigidbody2D rb;

    private enum NPCState { Patrol, Chase }
    private NPCState state = NPCState.Patrol;

    // NPC�nin anl�k bak�� y�n� ve son bilinen y�n� (patrol esnas�nda korunur)
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

        // Yer kontrol�
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        switch (state)
        {
            case NPCState.Patrol:
                Patrol();
                // �leriye y�nelik 2 raycast ile oyuncu alg�lama
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

    // Devriye modunda belirlenen noktalar aras�nda hareket
    void Patrol()
    {
        if (patrolPoints.Length == 0)
            return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];

        if (Vector2.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            // Hedefe do�ru giderken bak�� y�n�n� g�ncelle
            facingDirection = (targetPoint.position.x - transform.position.x) >= 0 ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;
        }
        else
        {
            // Noktaya ula��ld���nda son y�n korunur ve sonraki patrol noktas�na ge�ilir
            facingDirection = lastFacingDirection;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, patrolSpeed * Time.deltaTime);
    }

    // �leriye y�nelik 2 raycast (�st ve alt) kullanarak oyuncu alg�lamas�
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

    // Kovalama ve sald�r� durumu
    void ChaseAndAttack()
    {
        float deltaX = player.position.x - transform.position.x;
        bool detected = IsPlayerDetected();

        if (detected)
        {
            // E�er oyuncu NPC�nin bakt��� y�nde ise, chase belle�ini s�f�rla ve y�n g�ncelle
            chaseTimer = chaseMemoryTime;
            chaseDirectionSign = (deltaX >= 0) ? 1 : -1;

            // Oyuncuya do�ru hareket et (sadece yatay)
            Vector2 newPos = transform.position;
            newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
            transform.position = newPos;

            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;
        }
        else
        {
            // E�er oyuncu ileriye d�n�k raycast�te tespit edilemiyorsa, bellekte kalan s�re boyunca
            // �nceki y�n bilgisiyle (chaseDirectionSign) hareket etmeye devam et.
            chaseTimer -= Time.deltaTime;
            if (chaseTimer > 0f)
            {
                Vector2 newPos = transform.position;
                newPos.x += chaseSpeed * Time.deltaTime * chaseDirectionSign;
                transform.position = newPos;
            }
        }

        // Y�kseklik fark� varsa ve NPC yerdeyse z�plama
        float verticalDifference = player.position.y - transform.position.y;
        if (verticalDifference > 0.5f && isGrounded)
        {
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }

        // Sald�r�: Oyuncu attackRange i�indeyse sald�r, ard�ndan attackCooldown s�resi bekle
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

        // E�er chase belle�i s�resi dolduysa, devriye moduna d�n
        if (chaseTimer <= 0f)
        {
            state = NPCState.Patrol;
        }
    }

    // Sald�r� i�lemi (�rne�in: hasar verme, animasyon oynatma vb.)
    void AttackPlayer()
    {
        Debug.Log("Player'a sald�r�ld�!");
    }

    // Se�ildi�inde, NPC�nin bakt��� y�nde 2 raycast (�st ve alt) g�steren Gizmo �izimi
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        Gizmos.DrawLine(originUpper, originUpper + facingDirection * detectionRange);
        Gizmos.DrawLine(originLower, originLower + facingDirection * detectionRange);
    }
}
