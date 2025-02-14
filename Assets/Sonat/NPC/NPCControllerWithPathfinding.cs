using UnityEngine;
using System.Collections.Generic;

public class NPCControllerWithPathfinding : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3f;
    public float jumpForce = 5f;
    public float maxJumpHeight = 2f;      // NPC’nin zýplayabileceði maksimum yükseklik farký
    public float maxJumpDistance = 2f;    // NPC’nin atlayabileceði maksimum yatay mesafe

    [Header("Algýlama ve Saldýrý")]
    public float detectionRange = 5f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;     // NPC, saldýrý yapýnca 1 saniye bekleyecek
    private float attackTimer = 0f;
    public float detectionRayOffset = 0.5f;  // Raycast için dikey ofset
    public LayerMask detectionLayerMask;     // Kendi collider’ýnýzý veya istemediðiniz layer’larý hariç tutun

    [Header("Chase Bellek Süresi")]
    public float chaseMemoryTime = 2f;
    private float chaseTimer = 0f;
    private int chaseDirectionSign = 0;

    [Header("Devriye Noktalarý")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Yer Kontrolü")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Pathfinding (Platformlar Arasý)")]
    // Sahnedeki tüm platform node’larýný Inspector üzerinden atayabilir veya FindObjectsOfType ile alabilirsiniz.
    public PlatformNode[] platformNodes;
    private List<PlatformNode> currentPath;
    private int currentPathIndex = 0;

    // Referanslar
    private Transform player;
    private Rigidbody2D rb;

    // NPC durumlarý: Patrol, Chase veya Pathfinding
    private enum NPCState { Patrol, Chase, Pathfinding }
    private NPCState state = NPCState.Patrol;

    // NPC’nin baktýðý yön bilgileri (raycast ve hareket için)
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

        // Eðer oyuncu NPC’den belirgin þekilde farklý bir yüksekliðe çýktýysa (platform geçiþi)
        if (Mathf.Abs(player.position.y - transform.position.y) > 0.5f)
        {
            if (state != NPCState.Pathfinding)
            {
                PlatformNode npcNode = FindClosestPlatformNode(transform.position);
                PlatformNode playerNode = FindClosestPlatformNode(player.position);
                if (npcNode != null && playerNode != null)
                {
                    List<PlatformNode> path = FindPath(npcNode, playerNode);
                    if (path != null && path.Count > 0)
                    {
                        currentPath = path;
                        currentPathIndex = 0;
                        state = NPCState.Pathfinding;
                    }
                    else
                    {
                        state = NPCState.Chase; // Eðer rota bulunamazsa fallback
                    }
                }
            }
        }
        else
        {
            // Ayný zemin üzerinde ise pathfinding modundan çýk
            if (state == NPCState.Pathfinding)
                state = NPCState.Chase;
        }

        // Durumlara göre davranýþ
        switch (state)
        {
            case NPCState.Patrol:
                Patrol();
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
            case NPCState.Pathfinding:
                FollowPath();
                break;
        }
    }

    // Devriye: Belirlenmiþ noktalar arasýnda hareket
    void Patrol()
    {
        if (patrolPoints.Length == 0)
            return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];
        if (Vector2.Distance(transform.position, targetPoint.position) > 0.1f)
        {
            facingDirection = (targetPoint.position.x - transform.position.x) >= 0 ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;
        }
        else
        {
            facingDirection = lastFacingDirection;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        transform.position = Vector2.MoveTowards(transform.position, targetPoint.position, patrolSpeed * Time.deltaTime);
    }

    // Ýleriye yönelik 2 raycast (üst ve alt) ile oyuncu algýlama
    bool IsPlayerDetected()
    {
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        RaycastHit2D hitUpper = Physics2D.Raycast(originUpper, facingDirection, detectionRange, detectionLayerMask);
        RaycastHit2D hitLower = Physics2D.Raycast(originLower, facingDirection, detectionRange, detectionLayerMask);
        return (hitUpper.collider != null && hitUpper.collider.CompareTag("Player")) ||
               (hitLower.collider != null && hitLower.collider.CompareTag("Player"));
    }

    // Ayný zemin üzerindeki Chase ve Saldýrý davranýþý
    void ChaseAndAttack()
    {
        float deltaX = player.position.x - transform.position.x;
        bool detected = IsPlayerDetected();
        if (detected)
        {
            chaseTimer = chaseMemoryTime;
            chaseDirectionSign = (deltaX >= 0) ? 1 : -1;
            // Eðer oyuncu saldýrý mesafesinden uzaktaysa yaklaþ
            if (Mathf.Abs(deltaX) > attackRange)
            {
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
                facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
                lastFacingDirection = facingDirection;
            }
            else
            {
                // Saldýrý mesafesinde iken hareket etme; 1 saniyelik bekleme (attackCooldown) süresi boyunca dur
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
        }
        else
        {
            chaseTimer -= Time.deltaTime;
            if (chaseTimer > 0f)
            {
                Vector2 newPos = transform.position;
                newPos.x += chaseSpeed * Time.deltaTime * chaseDirectionSign;
                transform.position = newPos;
            }
        }
        if (chaseTimer <= 0f)
            state = NPCState.Patrol;
    }

    // Platformlar arasý hesaplanmýþ rota üzerinden ilerleme (pathfinding modunda)
    void FollowPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count)
        {
            state = NPCState.Chase;
            return;
        }
        PlatformNode targetNode = currentPath[currentPathIndex];
        Vector2 targetPos = targetNode.transform.position;
        // Yatay hareket: hedef node’a doðru
        Vector2 newPos = transform.position;
        newPos.x = Mathf.MoveTowards(transform.position.x, targetPos.x, chaseSpeed * Time.deltaTime);
        transform.position = newPos;
        // Hedefe yaklaþýldýðýnda
        if (Mathf.Abs(transform.position.x - targetPos.x) < 0.1f)
        {
            // Eðer hedef daha yukarýysa ve NPC yerdeyse atlamayý uygula
            if (targetPos.y > transform.position.y && isGrounded)
            {
                float heightDiff = targetPos.y - transform.position.y;
                float horizontalDiff = Mathf.Abs(targetPos.x - transform.position.x);
                if (heightDiff <= maxJumpHeight && horizontalDiff <= maxJumpDistance)
                    rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            }
            currentPathIndex++;
        }
        if (currentPathIndex >= currentPath.Count)
            state = NPCState.Chase;
    }

    // Verilen pozisyona en yakýn platform node’unu bulur
    PlatformNode FindClosestPlatformNode(Vector3 pos)
    {
        PlatformNode closest = null;
        float minDist = Mathf.Infinity;
        foreach (PlatformNode node in platformNodes)
        {
            float dist = Vector2.Distance(pos, node.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }
        return closest;
    }

    // Basit BFS kullanarak platform node’larý arasýnda rota (path) oluþturur
    List<PlatformNode> FindPath(PlatformNode start, PlatformNode target)
    {
        Queue<PlatformNode> queue = new Queue<PlatformNode>();
        Dictionary<PlatformNode, PlatformNode> cameFrom = new Dictionary<PlatformNode, PlatformNode>();
        queue.Enqueue(start);
        cameFrom[start] = null;
        while (queue.Count > 0)
        {
            PlatformNode current = queue.Dequeue();
            if (current == target)
                break;
            foreach (PlatformNode neighbor in current.neighbors)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    float verticalDiff = Mathf.Abs(neighbor.transform.position.y - current.transform.position.y);
                    float horizontalDiff = Mathf.Abs(neighbor.transform.position.x - current.transform.position.x);
                    if (verticalDiff <= maxJumpHeight && horizontalDiff <= maxJumpDistance)
                    {
                        queue.Enqueue(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }
        }
        if (!cameFrom.ContainsKey(target))
            return null; // Rota bulunamadý

        List<PlatformNode> path = new List<PlatformNode>();
        PlatformNode node = target;
        while (node != null)
        {
            path.Add(node);
            node = cameFrom[node];
        }
        path.Reverse();
        return path;
    }

    void AttackPlayer()
    {
        Debug.Log("Player'a saldýrýldý!");
        // Burada oyuncuya hasar verme veya animasyon tetikleme kodlarý eklenebilir.
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
