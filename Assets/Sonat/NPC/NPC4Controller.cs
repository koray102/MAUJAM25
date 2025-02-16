using Unity.VisualScripting;
using UnityEngine;

public class NPC4Controller : NPCBase
{
    public ParticleSystem MetalImpact;
    public Transform CarpismaTransformu;
    public bool KalkanSolEldeMi = false;

    // Animator referansý ekleniyor.
   

    void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
    }

    protected override void Patrol()
    {
        // Sprite flip durumuna göre kalkanýn sol elde olup olmadýðý ayarlanýyor.
        if (spriteRenderer.flipX)
            KalkanSolEldeMi = true;
        else
            KalkanSolEldeMi = false;

        // Patrol animasyonu tetikleniyor.
        

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

    protected override void ChaseAndAttack()
    {
        if (spriteRenderer.flipX)
            KalkanSolEldeMi = true;
        else
            KalkanSolEldeMi = false;

        float deltaX = player.position.x - transform.position.x;
        bool detected = IsPlayerDetected();

        if (detected)
        {
            // Chase animasyonu tetikleniyor.
         

            chaseTimer = chaseMemoryTime;
            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;

            if (Mathf.Abs(deltaX) > attackRange)
            {
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else
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
        }
        else
        {
            chaseTimer -= Time.deltaTime;
            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;

            if (chaseTimer > 0f && Mathf.Abs(deltaX) > attackRange)
            {
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else if (chaseTimer <= 0f)
            {
                state = NPCState.Patrol;
            }
        }
    }

    protected override void AttackPlayer()
    {
        // Attack animasyonu tetikleniyor.
      
        Debug.Log("NPC1: Player'a saldýrýldý!");
    }

    public override void GetDamage()
    {
        // Eðer oyuncu, NPC'nin baktýðý yönde deðilse (shield aktif deðilse) ölecek.
        if (!((!spriteRenderer.flipX && player.position.x > transform.position.x) ||
              (spriteRenderer.flipX && player.position.x < transform.position.x)))
        {
      
            Destroy(gameObject);
        }
        else
        {
            Instantiate(MetalImpact, CarpismaTransformu.position, Quaternion.identity);
        }
    }
}
