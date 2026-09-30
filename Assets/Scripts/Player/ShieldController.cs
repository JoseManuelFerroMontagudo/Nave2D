using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class ShieldController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionReference shieldAction;

    [Header("Energía")]
    [SerializeField] float maxEnergy = 100f;
    [SerializeField] float drainRate = 25f;
    [SerializeField] float regenRate = 10f;
    [SerializeField] float overloadCooldown = 3f;

    [Header("Rotación")]
    [SerializeField] bool spinWhileActive = true;
    [SerializeField] float spinDegreesPerSecond = 60f;
    [SerializeField] float impactAngleOffset = 180f;
    [SerializeField] float impactHoldSeconds = 0.6f;

    enum Phase { Off, Active, Deactivating, Destroying }
    Phase phase = Phase.Off;

    float energy;
    float cooldownRemaining;

    Animator animator;
    SpriteRenderer shieldVisual;
    Collider2D shieldCollider;

    float spinAngle;          // ángulo acumulado del giro continuo
    float impactAngle;        // ángulo objetivo cuando hay impacto
    float impactTimer;        // >0 significa "estoy orientado al impacto"

    static readonly int P_IsActive = Animator.StringToHash("IsActive");
    static readonly int P_Impact = Animator.StringToHash("Impact");
    static readonly int P_Destroy = Animator.StringToHash("Destroy");

    void Awake()
    {
        animator = GetComponent<Animator>();
        shieldVisual = GetComponent<SpriteRenderer>();
        shieldCollider = GetComponent<Collider2D>();

        energy = maxEnergy;
        SetShieldVisible(false);
    }

    void Update()
    {
        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;
        if (impactTimer > 0f) impactTimer -= Time.deltaTime;

        bool held = shieldAction != null && shieldAction.action.IsPressed();

        if (phase == Phase.Off || phase == Phase.Deactivating)
        {
            if (energy < maxEnergy)
                energy = Mathf.Min(maxEnergy, energy + regenRate * Time.deltaTime);
        }

        switch (phase)
        {
            case Phase.Off:
                if (held && cooldownRemaining <= 0f && energy > 0f)
                    Activate();
                break;

            case Phase.Active:
                if (!held) Deactivate();
                else
                {
                    energy -= drainRate * Time.deltaTime;
                    if (energy <= 0f)
                    {
                        energy = 0f;
                        Overload();
                    }
                }
                break;
        }
    }

    // LateUpdate corre DESPUÉS del Animator -> nuestra rotación gana
    void LateUpdate()
    {
        if (impactTimer > 0f)
        {
            // Orientado al impacto
            transform.rotation = Quaternion.Euler(0f, 0f, impactAngle);
        }
        else if (spinWhileActive && phase == Phase.Active)
        {
            // Giro continuo (lo que antes hacía la animación)
            spinAngle += spinDegreesPerSecond * Time.deltaTime;
            spinAngle %= 360f;
            transform.rotation = Quaternion.Euler(0f, 0f, spinAngle);
        }
    }

    void Activate()
    {
        phase = Phase.Active;
        spinAngle = 0f;        // reinicia el giro al activar
        impactTimer = 0f;
        SetShieldVisible(true);
        animator.SetBool(P_IsActive, true);
    }

    void Deactivate()
    {
        phase = Phase.Deactivating;
        animator.SetBool(P_IsActive, false);
        shieldCollider.enabled = false;
    }

    void Overload()
    {
        phase = Phase.Destroying;
        cooldownRemaining = overloadCooldown;
        animator.SetBool(P_IsActive, false);
        animator.SetTrigger(P_Destroy);
        shieldCollider.enabled = false;
    }

    public void AE_ActivateFinished() { }

    public void AE_DeactivateFinished()
    {
        if (phase == Phase.Deactivating)
        {
            SetShieldVisible(false);
            phase = Phase.Off;
        }
    }

    public void AE_DestroyFinished()
    {
        if (phase == Phase.Destroying)
        {
            SetShieldVisible(false);
            phase = Phase.Off;
        }
    }

    void SetShieldVisible(bool on)
    {
        shieldVisual.enabled = on;
        shieldCollider.enabled = on;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (phase != Phase.Active) return;
        if (!other.CompareTag("Bullet")) return;

        Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        impactAngle = angle + impactAngleOffset;
        impactTimer = impactHoldSeconds;   // durante este rato el escudo mira al impacto

        animator.SetTrigger(P_Impact);
        Destroy(other.gameObject);  // destruye la bala al impactar con el escudo
    }
}