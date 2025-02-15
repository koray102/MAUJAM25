using UnityEngine;

public class NPC1Controller : NPCBase
{
    protected override void Patrol()
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

    protected override void ChaseAndAttack()
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

            // Eðer oyuncu saldýrý menzilinin dýþýndaysa, ancak menzile yakýnsa, NPC'yi oyuncunun x pozisyonuna doðru hareket ettir
            if (Mathf.Abs(deltaX) > attackRange)
            {
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
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

            // NPC'nin bakýþ yönünü belirle
            facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
            lastFacingDirection = facingDirection;

            // Chase zamanlayýcýsý sýfýrdan büyükse, NPC'yi oyuncunun son bilinen x pozisyonuna doðru hareket ettir
            if (chaseTimer > 0f && Mathf.Abs(deltaX) > attackRange)
            {
                Vector2 newPos = transform.position;
                newPos.x = Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime);
                transform.position = newPos;
            }
            else if (chaseTimer > 0f)
            {

            }
            else
            {
                // Chase zamanlayýcýsý sýfýra ulaþtýysa, devriye moduna geri dön
                state = NPCState.Patrol;
            }
        }
    }

    protected override void AttackPlayer()
    {
        Debug.Log("NPC1: Player'a saldýrýldý!");
    }
}
