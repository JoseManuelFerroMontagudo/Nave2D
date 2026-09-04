using UnityEngine;
using System.Collections;

/// <summary>
/// Proyectil del Evil Wizard.
/// Viaja por travelTime segundos, luego suavemente desacelera,
/// se detiene un instante, y regresa al origen.
/// Usa TIEMPO (no distancia) para ser confiable con todos los modos.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WizardFireball : MonoBehaviour {

	public enum MoveMode { Straight, Wave, Homing, Decelerate }
	public MoveMode moveMode = MoveMode.Straight;

	public Vector2 direction;
	public float speed = 8f;
	public float damage = 2f;
	public float travelTime = 2.5f;     // Segundos volando hacia afuera
	public float lifetime = 14f;
	public Color projectileColor = Color.white; // Color del proyectil

	// --- Wave ---
	public float waveAmplitude = 2f;
	public float waveFrequency = 8f;

	// --- Homing ---
	public float homingStrength = 1.5f;

	// --- Decelerate ---
	public float decelTime = 0.6f;
	public float accelMultiplier = 2f;

	// --- Internos ---
	private float gracePeriod = 0.15f;
	private bool canHitWalls = false;
	private Rigidbody2D rb;
	private float aliveTime = 0f;
	private Transform playerTransform;
	private Vector3 spawnPos;
	private bool isReturning = false;
	private bool isBraking = false;
	private float currentSpeed;

	void Awake() {
		rb = GetComponent<Rigidbody2D>();
	}

	void Start() {
		if (rb == null) { Destroy(gameObject); return; } // Prefab sin Rigidbody2D
		rb.gravityScale = 0f;
		spawnPos = transform.position;
		currentSpeed = speed;
		Destroy(gameObject, lifetime);
		StartCoroutine(EnableWallCollision());

		// Aplicar color al sprite
		SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
		if (sr != null) {
			sr.color = projectileColor;
		}

		GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
		if (playerObj != null) playerTransform = playerObj.transform;

		// Iniciar ciclo de vida: volar → frenar → regresar
		StartCoroutine(LifeCycle());
	}

	IEnumerator EnableWallCollision() {
		yield return new WaitForSeconds(gracePeriod);
		canHitWalls = true;
	}

	// === CICLO DE VIDA: volar → frenar → regresar ===
	IEnumerator LifeCycle() {
		// FASE 1: Volar hacia afuera por travelTime segundos
		yield return new WaitForSeconds(travelTime);

		// FASE 2: Frenar suavemente (0.4 segundos)
		isBraking = true;
		float brakeTime = 0.4f;
		float elapsed = 0f;
		float startSpeed = currentSpeed;
		while (elapsed < brakeTime) {
			currentSpeed = Mathf.Lerp(startSpeed, 0f, elapsed / brakeTime);
			elapsed += Time.deltaTime;
			yield return null;
		}
		currentSpeed = 0f;
		rb.linearVelocity = Vector2.zero;

		// FASE 3: Pausa breve (la forma se ve)
		yield return new WaitForSeconds(0.25f);

		// FASE 4: Invertir y regresar
		isBraking = false;
		isReturning = true;
		direction = -direction;
		canHitWalls = false;
		currentSpeed = speed * 0.7f; // Regresa más lento

		// Rotar sprite
		float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, rot);
	}

	void FixedUpdate() {
		if (rb == null) return;
		aliveTime += Time.fixedDeltaTime;

		// Si está frenando, el LifeCycle coroutine maneja la velocidad
		if (isBraking) {
			rb.linearVelocity = direction.normalized * currentSpeed;
			return;
		}

		// Chequear si ya regresó al origen
		if (isReturning) {
			float distToOrigin = Vector2.Distance(transform.position, spawnPos);
			if (distToOrigin < 1f) {
				Destroy(gameObject);
				return;
			}
		}

		// Aplicar movimiento
		switch (moveMode) {
			case MoveMode.Straight:
				rb.linearVelocity = direction.normalized * currentSpeed;
				break;

			case MoveMode.Wave:
				Vector2 baseVel = direction.normalized * currentSpeed;
				Vector2 perp = new Vector2(-direction.y, direction.x).normalized;
				float wave = Mathf.Sin(aliveTime * waveFrequency) * waveAmplitude;
				rb.linearVelocity = baseVel + perp * wave;
				break;

			case MoveMode.Homing:
				if (playerTransform != null && !isReturning) {
					Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
					direction = Vector2.Lerp(direction.normalized, toPlayer, homingStrength * Time.fixedDeltaTime).normalized;
				}
				rb.linearVelocity = direction.normalized * currentSpeed;
				float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
				transform.rotation = Quaternion.Euler(0, 0, angle);
				break;

			case MoveMode.Decelerate:
				rb.linearVelocity = direction.normalized * currentSpeed;
				break;
		}
	}

	void OnTriggerEnter2D(Collider2D other) {
		if (other.CompareTag("Player")) {
			// Notificar a la nave del impacto
			PlayerShip2D ship = other.GetComponent<PlayerShip2D>();
			if (ship != null) {
				ship.ApplyDamage(damage);
			}
			Destroy(gameObject);
		}
		else if (other.CompareTag("Enemy")) {
			// Ignorar
		}
		else if (canHitWalls) {
			// Pared → rebotar (invertir dirección)
			direction = -direction;
			canHitWalls = false; // Solo un rebote
			float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0, 0, rot);
		}
	}
}
