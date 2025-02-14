using UnityEngine;
using System.Collections.Generic;

public class NPCControllerWithPathfinding : MonoBehaviour
{
    [Header("Hareket Ayarlar�")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3f;
    public float jumpForce = 5f;
    public float maxJumpHeight = 2f;      // NPC�nin z�playabilece�i maksimum y�kseklik fark�
    public float maxJumpDistance = 2f;    // NPC�nin atlayabilece�i maksimum yatay mesafe

    [Header("Alg�lama ve Sald�r�")]
    public float detectionRange = 5f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;     // NPC, sald�r� yap�nca 1 saniye bekleyecek
    private float attackTimer = 0f;
    public float detectionRayOffset = 0.5f;  // Raycast i�in dikey ofset
    public LayerMask detectionLayerMask;     // Kendi collider��n�z� veya istemedi�iniz layer�lar� hari� tutun

    [Header("Chase Bellek S�resi")]
    public float chaseMemoryTime = 2f;
    private float chaseTimer = 0f;
    private int chaseDirectionSign = 0;

    [Header("Devriye Noktalar�")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Yer Kontrol�")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Pathfinding (Platformlar Aras�)")]
    // Sahnedeki t�m platform node�lar�n� Inspector �zerinden atayabilir veya FindObjectsOfType ile alabilirsiniz.
    public PlatformNode[] platformNodes;
    private List<PlatformNode> currentPath;
    private int currentPathIndex = 0;

    // Referanslar
    private Transform player;
    private Rigidbody2D rb;

    // NPC durumlar�: Patrol, Chase veya Pathfinding
    private enum NPCState { Patrol, Chase, Pathfinding }
    private NPCState state = NPCState.Patrol;

    // NPC�nin bakt��� y�n bilgileri (raycast ve hareket i�in)
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

        // E�er oyuncu NPC�den belirgin �ekilde farkl� bir y�ksekli�e ��kt�ysa (platform ge�i�i)
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
                        state = NPCState.Chase; // E�er rota bulunamazsa fallback
                    }
                }
            }
        }
        else
        {
            // Ayn� zemin �zerinde ise pathfinding modundan ��k
            if (state == NPCState.Pathfinding)
                state = NPCState.Chase;
        }

        // Durumlara g�re davran��
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

    // Devriye: Belirlenmi� noktalar aras�nda hareket
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

    // �leriye y�nelik 2 raycast (�st ve alt) ile oyuncu alg�lama
    bool IsPlayerDetected()
    {
        Vector2 originUpper = (Vector2)transform.position + Vector2.up * detectionRayOffset;
        Vector2 originLower = (Vector2)transform.position - Vector2.up * detectionRayOffset;
        RaycastHit2D hitUpper = Physics2D.Raycast(originUpper, facingDirection, detectionRange, detectionLayerMask);
        RaycastHit2D hitLower = Physics2D.Raycast(originLower, facingDirection, detectionRange, detectionLayerMask);
        return (hitUpper.collider != null && hitUpper.collider.CompareTag("Player")) ||
               (hitLower.collider != null && hitLower.collider.CompareTag("Player"));
    }

    // Ayn� zemin �zerindeki Chase ve Sald�r� davran���
    void ChaseAndAttack()
    {
        float deltaX = player.position.x - transform.position.x;
        bool detected = IsPlayerDetected();
        if (detected)
        {
            chaseTimer = chaseMemoryTime;
            chaseDirectionSign = (deltaX >= 0) ? 1 : -1;
            // E�er oyuncu sald�r� mesafesinden uzaktaysa yakla�
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
                // Sald�r� mesafesinde iken hareket etme; 1 saniyelik bekleme (attackCooldown) s�resi boyunca dur
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

    // Platformlar aras� hesaplanm�� rota �zerinden ilerleme (pathfinding modunda)
    void FollowPath()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count)
        {
            state = NPCState.Chase;
            return;
        }
        PlatformNode targetNode = currentPath[currentPathIndex];
        Vector2 targetPos = targetNode.transform.position;
        // Yatay hareket: hedef node�a do�ru
        Vector2 newPos = transform.position;
        newPos.x = Mathf.MoveTowards(transform.position.x, targetPos.x, chaseSpeed * Time.deltaTime);
        transform.position = newPos;
        // Hedefe yakla��ld���nda
        if (Mathf.Abs(transform.position.x - targetPos.x) < 0.1f)
        {
            // E�er hedef daha yukar�ysa ve NPC yerdeyse atlamay� uygula
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

    // Verilen pozisyona en yak�n platform node�unu bulur
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

    // Basit BFS kullanarak platform node�lar� aras�nda rota (path) olu�turur
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
            return null; // Rota bulunamad�

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
        Debug.Log("Player'a sald�r�ld�!");
        // Burada oyuncuya hasar verme veya animasyon tetikleme kodlar� eklenebilir.
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
