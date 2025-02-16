using UnityEngine;

public class NPC3Controller : NPCBase
{
    // NPC-3: Bu NPC sabit durur, sadece oyuncu saldýrý menziline geldiðinde saldýrýr.

    protected override void Patrol()
    {
        // NPC-3, devriye (patrol) sýrasýnda hareket etmez.
        // Sadece oyuncunun x konumuna göre yüzünü ayarlar.
        float deltaX = player.position.x - transform.position.x;
        facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
        lastFacingDirection = facingDirection;
        // Hareket etmiyor; pozisyon sabit kalýyor.
    }

    protected override void ChaseAndAttack()
    {
        // NPC-3, chase moduna geçtiðinde da ayný yerde kalýr.
        // Yalnýzca oyuncunun x pozisyonuna göre yüzünü ayarlar ve
        // eðer oyuncu attackRange içinde ise saldýrýr.
        float deltaX = player.position.x - transform.position.x;
        float absDeltaX = Mathf.Abs(deltaX);

        facingDirection = (deltaX >= 0) ? Vector2.right : Vector2.left;
        lastFacingDirection = facingDirection;

        if (absDeltaX <= attackRange)
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
        // Eðer oyuncu menzile girmemiþse, NPC sadece yüzünü oyuncuya döndürür.
    }

    protected override void AttackPlayer()
    {

        Debug.Log("NPC-3: Player'a saldýrýldý!");
        // Buraya oyuncuya hasar verme veya saldýrý animasyonu eklenebilir.
    }

    public override void GetDamage()
    {

        Destroy(gameObject);
    }
}
