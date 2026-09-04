using UnityEngine;
using System.Collections;

public class EvilWizard : MonoBehaviour {

	[Header("Stats")]
	public float life = 50f;
	public float speed = 2f;
	public float attackRange = 10f;
	public float stopRange = 4f;

	[Header("Bullet Hell Settings")]
	public GameObject fireballPrefab;
	public Transform firePoint;

	[Header("Bullet Settings")]
	public float bulletSpeed = 9f;
	public float bulletTravelTime = 2.5f; // Segundos de vuelo antes de regresar

	[Header("Bullet Colors")]
	public Color colorRain = new Color(0.7f, 0.2f, 1f, 0.7f); // Morado semitransparente (Fondo)
	public Color colorSpiral = Color.cyan;
	public Color colorLaser = Color.red;
	public Color colorPetal = Color.magenta;
	public Color colorShotgun = Color.yellow;
	public Color colorWave = Color.green;
	public Color colorZigzag = new Color(1f, 0.5f, 0f, 1f); // Naranja

	[Header("Circle Rain (capa de fondo constante)")]
	public int rainBullets = 10;          // Balas por anillo
	public float rainInterval = 0.35f;    // Cada cuánto dispara un anillo
	[Range(0.3f, 1f)]
	public float rainSpeedMult = 0.55f;   // Velocidad relativa (más lento = fondo)

	// --- Estado interno ---
	private bool isDead = false;
	private bool isAttacking = false;
	private bool facingRight = true;
	private bool isHitted = false;
	public bool isInvincible = false;

	private Transform player;
	private Rigidbody2D rb;
	private Animator anim;

	private int patternIndex = 0;
	private bool secondaryDone = false;

	void Awake () {
		rb = GetComponent<Rigidbody2D>();
		anim = GetComponent<Animator>(); // Puede ser null si la nave no tiene Animator
		SetAnim("IsMoving", false);
		SetAnim("IsAttacking", false);
		SetAnim("Hit", false);
		SetAnim("IsDead", false);
		
		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		if (playerObj != null) {
			player = playerObj.transform;
		}
	}

	// Helper segúro para Animator — funciona aunque no haya Animator en la nave
	void SetAnim(string param, bool value) {
		if (anim != null) anim.SetBool(param, value);
	}

	
	void FixedUpdate () {
		if (isDead || isHitted) return;
		if (player == null) return;

		float distanceToPlayer = Vector2.Distance(transform.position, player.position);

		if (distanceToPlayer < attackRange) {
			bool playerToRight = player.position.x > transform.position.x;
			if (playerToRight && !facingRight) Flip();
			else if (!playerToRight && facingRight) Flip();

			if (distanceToPlayer > stopRange && !isAttacking) {
				float dir = facingRight ? -1f : 1f;
				rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
				SetAnim("IsMoving", true);
			} else {
				rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
				SetAnim("IsMoving", false);
				if (!isAttacking) {
					StartCoroutine(AttackRoutine());
				}
			}
		} else {
			rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
			SetAnim("IsMoving", false);
		}
	}

	// =============================================
	// ATTACK ROUTINE — 7 combos
	// Cada combo: Lluvia de círculos + Patrón con forma clara
	// =============================================
	IEnumerator AttackRoutine() {
		isAttacking = true;
		isInvincible = true;
		SetAnim("IsAttacking", true);
		SetAnim("Hit", false);

		yield return new WaitForSeconds(0.5f);
		isInvincible = false;

		switch (patternIndex % 7) {
			case 0: yield return StartCoroutine(RunCombo(Pattern_TripleSpiral())); break;
			case 1: yield return StartCoroutine(RunCombo(Pattern_CrossLaser())); break;
			case 2: yield return StartCoroutine(RunCombo(Pattern_FlowerPetals())); break;
			case 3: yield return StartCoroutine(RunCombo(Pattern_AimedShotgun())); break;
			case 4: yield return StartCoroutine(RunCombo(Pattern_WaveWall())); break;
			case 5: yield return StartCoroutine(RunCombo(Pattern_ZigzagStorm())); break;
			case 6: yield return StartCoroutine(RunCombo(Pattern_GrandFinale())); break;
		}
		patternIndex++;

		SetAnim("IsAttacking", false);
		yield return new WaitForSeconds(1.8f);
		isAttacking = false;
	}

	// =============================================
	// RunCombo: Lluvia de círculos en paralelo al patrón
	// Los círculos son LENTOS y POCOS = fondo
	// El patrón es RÁPIDO y DENSO = protagonista
	// =============================================
	IEnumerator RunCombo(IEnumerator mainPattern) {
		secondaryDone = false;
		StartCoroutine(RunPattern(mainPattern));

		float angle = 0f;
		while (!secondaryDone) {
			// Lluvia de fondo: círculos lentos con pocas balas
			FireCircle(rainBullets, angle, bulletSpeed * rainSpeedMult, 
				WizardFireball.MoveMode.Straight, bulletTravelTime * 1.2f, colorRain);
			angle += 17f;
			yield return new WaitForSeconds(rainInterval);
		}
	}

	IEnumerator RunPattern(IEnumerator pattern) {
		yield return StartCoroutine(pattern);
		secondaryDone = true;
	}

	// =============================================
	// HELPER: Dispara un círculo de balas
	// =============================================
	void FireCircle(int count, float offset, float spd, WizardFireball.MoveMode mode, float travel, Color color) {
		if (fireballPrefab == null) return;
		float step = 360f / count;
		for (int i = 0; i < count; i++) {
			float deg = step * i + offset;
			Vector2 dir = new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
			SpawnBullet(dir, mode, spd, travel, color);
		}
	}

	// =============================================
	// PATRÓN 1: TRIPLE ESPIRAL
	// 3 brazos girando, disparan MUY rápido (0.02s)
	// para que los 3 brazos se vean clarísimo.
	// =============================================
	IEnumerator Pattern_TripleSpiral() {
		float angle = 0f;
		for (int i = 0; i < 50; i++) {
			for (int arm = 0; arm < 3; arm++) {
				float a = angle + arm * 120f;
				Vector2 dir = DegToDir(a);
				// Alternar Wave cada 5 balas para textura visual
				WizardFireball.MoveMode mode = (i % 5 == 0) 
					? WizardFireball.MoveMode.Wave : WizardFireball.MoveMode.Straight;
				SpawnBullet(dir, mode, bulletSpeed * 1.1f, bulletTravelTime, colorSpiral);
			}
			angle += 13f; // Rotación suave
			yield return new WaitForSeconds(0.025f); // MUY rápido → espiral densa y visible
		}
	}

	// =============================================
	// PATRÓN 2: CRUZ LÁSER GIRATORIA
	// 4 brazos gruesos (3 balas de ancho) girando 360°.
	// Cada brazo es una línea sólida que barre el espacio.
	// =============================================
	IEnumerator Pattern_CrossLaser() {
		float angle = 0f;
		for (int step = 0; step < 40; step++) {
			for (int arm = 0; arm < 4; arm++) {
				float armBase = angle + arm * 90f;
				// 3 balas por brazo (grosor)
				for (int t = -1; t <= 1; t++) {
					float a = armBase + t * 4f;
					SpawnBullet(DegToDir(a), WizardFireball.MoveMode.Straight, 
						bulletSpeed * 1.3f, bulletTravelTime, colorLaser);
				}
			}
			angle += 9f; // Gira 9° por paso
			yield return new WaitForSeconds(0.04f);
		}
	}

	// =============================================
	// PATRÓN 3: FLOR DE PÉTALOS
	// 8 pétalos de 6 balas cada uno. Cada pétalo es un
	// abanico que se dispara instantáneamente.
	// Las balas usan Wave con diferentes frecuencias
	// para crear curvas orgánicas visibles.
	// =============================================
	IEnumerator Pattern_FlowerPetals() {
		int petals = 8;
		int bulletsPerPetal = 7;
		float petalStep = 360f / petals;

		for (int p = 0; p < petals; p++) {
			float center = petalStep * p;
			
			// Dispara TODO el pétalo de golpe (instantáneo)
			for (int b = 0; b < bulletsPerPetal; b++) {
				float spread = ((float)b / (bulletsPerPetal - 1) - 0.5f) * 20f;
				Vector2 dir = DegToDir(center + spread);
				// Cada bala con Wave distinto → curva orgánica
				float amp = 1.5f + b * 0.5f;
				float freq = 4f + b * 1.2f;
				SpawnBulletWave(dir, bulletSpeed * (0.9f + b * 0.08f), 
					bulletTravelTime, amp, freq, colorPetal);
			}
			yield return new WaitForSeconds(0.08f); // Pausa corta entre pétalos
		}

		// Segunda oleada rotada 22.5° para llenar los huecos
		yield return new WaitForSeconds(0.15f);
		for (int p = 0; p < petals; p++) {
			float center = petalStep * p + petalStep * 0.5f;
			for (int b = 0; b < bulletsPerPetal; b++) {
				float spread = ((float)b / (bulletsPerPetal - 1) - 0.5f) * 20f;
				Vector2 dir = DegToDir(center + spread);
				SpawnBulletWave(dir, bulletSpeed * (0.9f + b * 0.08f), 
					bulletTravelTime, 1.5f + b * 0.5f, 4f + b * 1.2f, colorPetal);
			}
			yield return new WaitForSeconds(0.08f);
		}
	}

	// =============================================
	// PATRÓN 4: ESCOPETAZO DIRIGIDO
	// Ráfagas apuntando al jugador. Cada ráfaga: 12 balas
	// en abanico amplio. Se re-apunta entre ráfagas.
	// =============================================
	IEnumerator Pattern_AimedShotgun() {
		for (int burst = 0; burst < 8; burst++) {
			float aim = GetAngleToPlayer();

			// 12 balas en abanico de 50°
			for (int b = 0; b < 12; b++) {
				float spread = ((float)b / 11f - 0.5f) * 50f;
				Vector2 dir = DegToDir(aim + spread);
				float spd = bulletSpeed * Random.Range(1.0f, 1.4f);
				WizardFireball.MoveMode mode = (b % 4 == 0) 
					? WizardFireball.MoveMode.Wave : WizardFireball.MoveMode.Straight;
				SpawnBullet(dir, mode, spd, bulletTravelTime, colorShotgun);
			}

			// Cada 2 ráfagas, un mini-círculo sorpresa
			if (burst % 2 == 1) {
				FireCircle(8, Random.Range(0f, 45f), bulletSpeed, 
					WizardFireball.MoveMode.Straight, bulletTravelTime, colorShotgun);
			}

			yield return new WaitForSeconds(0.18f);
		}
	}

	// =============================================
	// PATRÓN 5: MURO DE ONDAS
	// Oleadas de balas en un arco amplio hacia el jugador.
	// Cada oleada usa Wave con amplitud creciente,
	// creando un muro ondulante imposible.
	// =============================================
	IEnumerator Pattern_WaveWall() {
		for (int wave = 0; wave < 6; wave++) {
			float aim = GetAngleToPlayer();

			// 10 balas cubriendo 120° hacia el jugador
			for (int b = 0; b < 10; b++) {
				float spread = ((float)b / 9f - 0.5f) * 120f;
				Vector2 dir = DegToDir(aim + spread);
				float amp = 2f + wave * 0.7f; // Amplitud crece por oleada
				float freq = 5f + b * 0.6f;
				SpawnBulletWave(dir, bulletSpeed, bulletTravelTime, amp, freq, colorWave);
			}

			yield return new WaitForSeconds(0.15f);
		}
	}

	// =============================================
	// PATRÓN 6: ZIGZAG STORM
	// Columnas de balas que alternan izquierda/derecha.
	// Cada oleada dispara más rápido. Muy denso.
	// =============================================
	IEnumerator Pattern_ZigzagStorm() {
		for (int wave = 0; wave < 10; wave++) {
			float aim = GetAngleToPlayer();
			float zigDir = (wave % 2 == 0) ? 35f : -35f;

			for (int b = 0; b < 10; b++) {
				float zig = (b % 2 == 0) ? zigDir : -zigDir;
				float a = aim + zig + b * 7f;
				Vector2 dir = DegToDir(a);
				float spd = bulletSpeed * (1f + wave * 0.1f);
				SpawnBullet(dir, WizardFireball.MoveMode.Straight, spd, bulletTravelTime, colorZigzag);
			}

			// Cada 3 oleadas, burst Wave circular
			if (wave % 3 == 2) {
				FireCircle(10, wave * 18f, bulletSpeed * 0.8f, 
					WizardFireball.MoveMode.Wave, bulletTravelTime, colorZigzag);
			}

			yield return new WaitForSeconds(0.07f);
		}
	}

	// =============================================
	// PATRÓN 7: GRAND FINALE
	// Combina espiral + shotgun + círculos + caos.
	// El ataque más largo y denso de todos.
	// =============================================
	IEnumerator Pattern_GrandFinale() {
		// Fase 1: Espiral rápida de 3 brazos
		float angle = 0f;
		for (int i = 0; i < 30; i++) {
			for (int arm = 0; arm < 3; arm++) {
				float a = angle + arm * 120f;
				SpawnBullet(DegToDir(a), WizardFireball.MoveMode.Straight, 
					bulletSpeed * 1.2f, bulletTravelTime, colorSpiral);
			}
			angle += 17f;
			yield return new WaitForSeconds(0.02f);
		}

		// Fase 2: Triple shotgun rápido al jugador
		for (int burst = 0; burst < 3; burst++) {
			float aim = GetAngleToPlayer();
			for (int b = 0; b < 14; b++) {
				float spread = ((float)b / 13f - 0.5f) * 55f;
				SpawnBullet(DegToDir(aim + spread), WizardFireball.MoveMode.Straight, 
					bulletSpeed * 1.3f, bulletTravelTime, colorShotgun);
			}
			yield return new WaitForSeconds(0.1f);
		}

		// Fase 3: Lluvia caótica express
		for (int i = 0; i < 25; i++) {
			float rndAngle = Random.Range(0f, 360f);
			WizardFireball.MoveMode mode = (i % 3 == 0) 
				? WizardFireball.MoveMode.Wave : WizardFireball.MoveMode.Straight;
			SpawnBullet(DegToDir(rndAngle), mode, 
				bulletSpeed * Random.Range(0.8f, 1.3f), bulletTravelTime, colorZigzag);
			yield return new WaitForSeconds(0.02f);
		}

		// Fase 4: MEGA BURST — 3 anillos superpuestos
		FireCircle(16, 0f, bulletSpeed * 1.1f, WizardFireball.MoveMode.Straight, bulletTravelTime, colorLaser);
		FireCircle(16, 11f, bulletSpeed * 0.9f, WizardFireball.MoveMode.Wave, bulletTravelTime, colorPetal);
		yield return new WaitForSeconds(0.1f);
		FireCircle(14, 7f, bulletSpeed * 1.2f, WizardFireball.MoveMode.Straight, bulletTravelTime, colorWave);
	}

	// =============================================
	// HELPERS
	// =============================================

	Vector2 DegToDir(float degrees) {
		return new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
	}

	float GetAngleToPlayer() {
		if (player == null) return 0f;
		Vector2 toPlayer = (player.position - transform.position).normalized;
		return Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
	}

	void SpawnBullet(Vector2 dir, WizardFireball.MoveMode mode, float spd, float travel, Color color) {
		Vector3 pos = firePoint != null ? firePoint.position : transform.position;
		GameObject bullet = Instantiate(fireballPrefab, pos, Quaternion.identity);

		WizardFireball fb = bullet.GetComponent<WizardFireball>();
		if (fb != null) {
			fb.direction = dir.normalized;
			fb.speed = spd;
			fb.moveMode = mode;
			fb.travelTime = travel;
			fb.projectileColor = color;
		}

		float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		bullet.transform.rotation = Quaternion.Euler(0, 0, rot);
	}

	void SpawnBulletWave(Vector2 dir, float spd, float travel, float amp, float freq, Color color) {
		Vector3 pos = firePoint != null ? firePoint.position : transform.position;
		GameObject bullet = Instantiate(fireballPrefab, pos, Quaternion.identity);

		WizardFireball fb = bullet.GetComponent<WizardFireball>();
		if (fb != null) {
			fb.direction = dir.normalized;
			fb.speed = spd;
			fb.moveMode = WizardFireball.MoveMode.Wave;
			fb.travelTime = travel;
			fb.waveAmplitude = amp;
			fb.waveFrequency = freq;
			fb.projectileColor = color;
		}

		float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		bullet.transform.rotation = Quaternion.Euler(0, 0, rot);
	}

	// =============================================
	// DAMAGE, DEATH
	// =============================================

	void Flip () {
		facingRight = !facingRight;
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	public void ApplyDamage(float damage) {
		if (!isInvincible && !isDead) {
			float dir = damage / Mathf.Abs(damage);
			damage = Mathf.Abs(damage);
			life -= damage;
			
			if (life <= 0) {
				Die();
			} else if (isAttacking) {
				StartCoroutine(DamageFlash());
			} else {
				SetAnim("Hit", true);
				rb.linearVelocity = Vector2.zero;
				rb.AddForce(new Vector2(dir * 400f, 100f));
				StartCoroutine(HitTime());
			}
		}
	}

	IEnumerator DamageFlash() {
		SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
		if (sr != null) {
			Color original = sr.color;
			sr.color = Color.red;
			yield return new WaitForSeconds(0.08f);
			sr.color = original;
		}
	}

	void Die() {
		isDead = true;
		SetAnim("IsDead", true);
		rb.linearVelocity = Vector2.zero;
		isAttacking = false;
		StopAllCoroutines();
		StartCoroutine(DestroyEnemy());
	}

	IEnumerator HitTime() {
		isHitted = true;
		isInvincible = true;
		yield return new WaitForSeconds(0.15f);
		isHitted = false;
		isInvincible = false;
		SetAnim("Hit", false);
	}

	IEnumerator DestroyEnemy() {
		CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
		if (capsule != null) {
			capsule.size = new Vector2(1f, 0.25f);
			capsule.offset = new Vector2(0f, -0.8f);
			capsule.direction = CapsuleDirection2D.Horizontal;
		}
		yield return new WaitForSeconds(0.25f);
		rb.linearVelocity = Vector2.zero;
		yield return new WaitForSeconds(3f);
		Destroy(gameObject);
	}

	void OnCollisionStay2D(Collision2D collision) {
		if (collision.gameObject.CompareTag("Player") && !isDead) {
			collision.gameObject.GetComponent<PlayerShip2D>()?.ApplyDamage(2f);
		}
	}
}
