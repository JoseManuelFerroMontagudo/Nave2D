using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class ShieldController : MonoBehaviour
{
    [Header("Input")]
    public bool escudoMantenido;

    [Header("Energía")]
    public int drenajePorSegundo = 25;
    public float enfriamientoSobrecarga = 3f;

    [Header("Rotación")]
    public bool girarMientrasActivo = true;
    public float gradosPorSegundo = 60f;
    public float desfaseAnguloImpacto = 180f;
    public float duracionImpacto = 0.6f;

    enum Fase { Apagado, Activo, Desactivando, Destruyendo }
    Fase fase = Fase.Apagado;
    public bool EstaActivo => fase == Fase.Activo;

    float enfriamientoRestante;
    float acumuladorDrenaje;
    float anguloGiro;
    float anguloImpacto;
    float tiempoImpacto;

    Animator animador;
    SpriteRenderer visual;
    Collider2D colisionador;
    Stats estadisticas;

    void Awake()
    {
        animador = GetComponent<Animator>();
        visual = GetComponent<SpriteRenderer>();
        colisionador = GetComponent<Collider2D>();
        estadisticas = GetComponentInParent<Stats>();

        if (estadisticas == null)
            Debug.LogError($"[ShieldController] No se encontró Stats en el padre de {name}");

        MostrarEscudo(false);
    }

    public void OnShield(InputValue valor) => escudoMantenido = valor.isPressed;

    void Update()
    {
        if (enfriamientoRestante > 0f) enfriamientoRestante -= Time.deltaTime;
        if (tiempoImpacto > 0f) tiempoImpacto -= Time.deltaTime;
        if (estadisticas == null) return;

        // Regeneración delegada a Stats (solo cuando NO está activo ni sobrecargado)
        if (fase == Fase.Apagado || fase == Fase.Desactivando)
            estadisticas.RegenerarEnergia(Time.deltaTime);

        if (fase == Fase.Apagado)
        {
            if (escudoMantenido && enfriamientoRestante <= 0f && estadisticas.TieneEnergia)
                Activar();
            return;
        }

        if (fase != Fase.Activo) return;

        if (!escudoMantenido) { Desactivar(); return; }

        acumuladorDrenaje += drenajePorSegundo * Time.deltaTime;
        int entero = Mathf.FloorToInt(acumuladorDrenaje);
        if (entero > 0)
        {
            estadisticas.DrenarEnergia(entero);
            acumuladorDrenaje -= entero;
        }

        if (estadisticas.energia <= 0) Sobrecargar();
    }

    void LateUpdate()
    {
        if (tiempoImpacto > 0f)
            transform.rotation = Quaternion.Euler(0f, 0f, anguloImpacto);
        else if (girarMientrasActivo && fase == Fase.Activo)
            transform.rotation = Quaternion.Euler(0f, 0f, anguloGiro += gradosPorSegundo * Time.deltaTime);
    }

    void Activar()
    {
        fase = Fase.Activo;
        anguloGiro = 0f;
        tiempoImpacto = 0f;
        acumuladorDrenaje = 0f;
        MostrarEscudo(true);
        animador.SetBool("IsActive", true);
    }

    void Desactivar()
    {
        fase = Fase.Desactivando;
        animador.SetBool("IsActive", false);
        colisionador.enabled = false;
    }

    void Sobrecargar()
    {
        fase = Fase.Destruyendo;
        enfriamientoRestante = enfriamientoSobrecarga;
        acumuladorDrenaje = 0f;
        animador.SetBool("IsActive", false);
        animador.SetTrigger("Destroy");
        colisionador.enabled = false;
    }

    public void TerminarEscudo()
    {
        if (fase != Fase.Desactivando && fase != Fase.Destruyendo) return;
        MostrarEscudo(false);
        fase = Fase.Apagado;
    }

    void MostrarEscudo(bool activo) => visual.enabled = colisionador.enabled = activo;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (fase != Fase.Activo) return;
        if (!otro.CompareTag("Bullet") &&
            !otro.CompareTag("Bullet1") &&
            !otro.CompareTag("Bullet2") &&
            !otro.CompareTag("Bullet3") &&
            !otro.CompareTag("Asteroide")) return;
        Vector2 dir = ((Vector2)otro.transform.position - (Vector2)transform.position).normalized;
        anguloImpacto = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + desfaseAnguloImpacto;
        tiempoImpacto = duracionImpacto;
        animador.SetTrigger("Impact");
        if (!otro.CompareTag("Asteroide")) Destroy(otro.gameObject);
    }
}