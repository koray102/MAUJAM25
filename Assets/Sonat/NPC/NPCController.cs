using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Hareket Ayarlar�")]
    public float patrolSpeed = 2f;       // Devriye h�z�
    public float chaseSpeed = 3f;        // Kovalama h�z�

    [Header("Alg�lama ve Sald�r�")]
    public float detectionRange = 5f;    // �leriye y�nelik alg�lama mesafesi
    public float attackRange = 1f;       // Sald�r� mesafesi
    public float attackCooldown = 1f;    // Sald�r� sonras� bekleme s�resi (1 saniye)
    private float attackTimer = 0f;
    public float detectionRayOffset = 0.5f;  // Raycast i�in dikey ofset
    public LayerMask detectionLayerMask;     // NPC�nin kendi collider��n� ya da istenmeyen layer�lar� g�rmezden gelmek i�in

    [Header("Chase Bellek S�resi")]
    public float chaseMemoryTime = 2f;   // Oyuncu belirli s�re alg�lanmazsa devriyeye d�nme s�resi
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

    // NPC�nin bak�� y�n� bilgisi (raycast ve hareket i�in)
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

        // Yer kontrol�
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

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

    // Devriye modunda belirlenmi� noktalar aras�nda hareket
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

    // �leriye y�nelik �st ve alt offset�li 2 raycast ile oyuncu alg�lama
    bool IsPlayerDetected()
    {
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        RaycastHit2D hitUpper = Physics2D.Raycast(originUpper, facingDirection, detectionRange, detectionLayerMask);
        RaycastHit2D hitLower = Physics2D.Raycast(originLower, facingDirection, detectionRange, detectionLayerMask);
        return (hitUpper.collider != null && hitUpper.collider.CompareTag("Player")) ||
               (hitLower.collider != null && hitLower.collider.CompareTag("Player"));
    }

    // Chase modunda, oyuncu e�er yukar�daysa NPC z�plam�yor; yaln�zca x eksenindeki hizaya gelmeye �al���yor
    void ChaseAndAttack()
    {
        // NPC ile oyuncu aras�ndaki yatay mesafeyi hesapla
        float deltaX = player.position.x - transform.position.x;

        // Oyuncunun tespit edilip edilmedi�ini kontrol et
        bool detected = IsPlayerDetected();

        if (detected)
        {
            // Chase zamanlay�c�s�n� s�f�rla
            chaseTimer = chaseMemoryTime;

            // NPC'nin bak�� y�n�n� belirle
            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;

            // E�er oyuncu sald�r� menzilinin d���ndaysa, NPC'yi oyuncunun x pozisyonuna do�ru hareket ettir
            if (Mathf.Abs(deltaX) > attackRange)
            {
                // Hedef x pozisyonunu hesapla (sald�r� menzilinin s�n�r�nda duracak �ekilde)
                float targetX = player.position.x - Mathf.Sign(deltaX) * attackRange;
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, targetX, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else
            {
                // Sald�r� menzilindeyse, sald�r� zamanlay�c�s�n� kontrol et
                if (attackTimer <= 0f)
                {
                    // Sald�r�y� ger�ekle�tir
                    AttackPlayer();
                    // Sald�r� sonras� bekleme s�resini ayarla
                    attackTimer = attackCooldown;
                }
                else
                {
                    // Sald�r� zamanlay�c�s�n� azalt
                    attackTimer -= Time.deltaTime;
                }
            }
        }
        else
        {
            // Chase zamanlay�c�s�n� azalt
            chaseTimer -= Time.deltaTime;

            // Chase zamanlay�c�s� s�f�rdan b�y�kse, NPC'yi oyuncunun son bilinen x pozisyonuna do�ru hareket ettir
            if (chaseTimer > 0f)
            {
                // Hedef x pozisyonunu hesapla (sald�r� menzilinin s�n�r�nda duracak �ekilde)
                float targetX = player.position.x - Mathf.Sign(deltaX) * attackRange;
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, targetX, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else
            {
                // Chase zamanlay�c�s� s�f�ra ula�t�ysa, devriye moduna geri d�n
                state = NPCState.Patrol;
            }
        }
    }





    void AttackPlayer()
    {
        Debug.Log("Player'a sald�r�ld�!");
        // Buraya oyuncuya hasar verme veya animasyon tetikleme kodlar�n� ekleyebilirsiniz.
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

