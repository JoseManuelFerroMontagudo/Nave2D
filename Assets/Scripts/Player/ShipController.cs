using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Stats))]
public class ShipController : MonoBehaviour
{
    [Header("Propulsión")]
    public float aceleracionEmpuje = 24f;
    public float velocidadMaxima = 20f;

    [Header("Rotación")]
    public float zonaMuertaRaton = 0.1f;
    public float velocidadRotacionMaxima = 120f;
    const float respuestaRotacion = 18f;

    [Header("Giro 180")]
    public float respuestaGiro = 10f;   // antes "velocidadGiro"; ahora es constante de suavizado
    public int costoGiro = 20;

    [Header("Impulso lateral")]
    public float velocidadImpulso = 40f;
    public float duracionImpulso = 0.15f;
    public int costoImpulso = 10;

    [Header("Freno")]
    public float desaceleracionFreno = 15f;
    public float umbralParadaFreno = 0.05f;

    Rigidbody2D cuerpo;
    Stats estadisticas;

    Vector2 entradaMovimiento;
    Vector2? posRatonPantalla;
    Vector2 velocidadBase, vectorImpulso;
    float rotacionObjetivo, objetivoGiro, tiempoImpulsoRestante;
    bool frenando, girando;

    void Start() => SceneLoader.Instance.LoadLevel("Nivel1");
    void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        estadisticas = GetComponent<Stats>();
        rotacionObjetivo = cuerpo.rotation;
    }

    // ================== INPUT ==================
    public void OnMove(InputValue valor) => entradaMovimiento = valor.Get<Vector2>();
    public void OnLook(InputValue valor) => posRatonPantalla = valor.Get<Vector2>();
    public void OnBrake(InputValue valor) { if (valor.isPressed) frenando = true; }

    public void OnUTurn(InputValue valor)
    {
        if (!valor.isPressed || girando || estadisticas == null || estadisticas.energia < costoGiro) return;
        estadisticas.DrenarEnergia(costoGiro);
        girando = true;
        frenando = false;
        rotacionObjetivo = objetivoGiro = cuerpo.rotation + 180f;
    }

    public void OnDash(InputValue valor)
    {
        float eje = valor.Get<float>();
        if (Mathf.Abs(eje) < 0.5f || estadisticas == null || estadisticas.energia < costoImpulso) return;
        estadisticas.DrenarEnergia(costoImpulso);
        Vector2 direccionLocal = eje < 0f ? Vector2.left : Vector2.right;
        vectorImpulso = cuerpo.transform.TransformDirection(direccionLocal) * velocidadImpulso;
        tiempoImpulsoRestante = duracionImpulso;
    }

    // ================== FÍSICA ==================
    void FixedUpdate()
    {
        ActualizarRotacion();
        ActualizarEmpujeYFreno();
        if (tiempoImpulsoRestante > 0f) tiempoImpulsoRestante -= Time.fixedDeltaTime;

        cuerpo.linearVelocity = velocidadBase + (tiempoImpulsoRestante > 0f ? vectorImpulso : Vector2.zero);
    }

    void ActualizarRotacion()
    {
        // El giro 180 tiene prioridad y corta el control por ratón.
        // Usa el mismo suavizado exponencial que la rotación normal.
        if (girando)
        {
            float suavizado = 1f - Mathf.Exp(-respuestaGiro * Time.fixedDeltaTime);
            cuerpo.MoveRotation(Mathf.LerpAngle(cuerpo.rotation, objetivoGiro, suavizado));

            if (Mathf.Abs(Mathf.DeltaAngle(cuerpo.rotation, objetivoGiro)) < 0.1f)
            {
                cuerpo.MoveRotation(objetivoGiro);
                rotacionObjetivo = objetivoGiro;
                girando = false;
            }
            return;
        }

        if (posRatonPantalla is Vector2 pos)
        {
            float mitadAncho = Screen.width * 0.5f;
            float desvio = (pos.x - mitadAncho) / mitadAncho;
            float magnitud = Mathf.Abs(desvio);
            if (magnitud > zonaMuertaRaton)
            {
                float factor = Mathf.Clamp01((magnitud - zonaMuertaRaton) / (1f - zonaMuertaRaton));
                rotacionObjetivo -= Mathf.Sign(desvio) * factor * velocidadRotacionMaxima * Time.fixedDeltaTime;
            }
            float suavizado = 1f - Mathf.Exp(-respuestaRotacion * Time.fixedDeltaTime);
            cuerpo.MoveRotation(Mathf.LerpAngle(cuerpo.rotation, rotacionObjetivo, suavizado));
        }
    }

    void ActualizarEmpujeYFreno()
    {
        if (!girando && entradaMovimiento.sqrMagnitude > 0.001f)
        {
            velocidadBase += (Vector2)cuerpo.transform.TransformDirection(entradaMovimiento.normalized)
                             * (aceleracionEmpuje * Time.fixedDeltaTime);
            frenando = false;
        }

        if (frenando)
        {
            float velocidad = velocidadBase.magnitude;
            if (velocidad <= umbralParadaFreno) { velocidadBase = Vector2.zero; frenando = false; }
            else velocidadBase = velocidadBase.normalized
                               * Mathf.Max(0f, velocidad - desaceleracionFreno * Time.fixedDeltaTime);
        }

        if (velocidadBase.magnitude > velocidadMaxima)
            velocidadBase = velocidadBase.normalized * velocidadMaxima;
    }
}