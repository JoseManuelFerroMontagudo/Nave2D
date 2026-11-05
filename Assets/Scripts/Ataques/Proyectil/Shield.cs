using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Shield : MonoBehaviour, IPermanenteHandler
{
    [Header("Energía")]
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

        MostrarEscudo(false);
    }

    // Llamado por AtaquePermanente.Actualizar cada frame.
    // boost ya incluye "hay energía suficiente" (lo calcula el runner).
    public void SetBoost(bool b)
    {
        if (b)
        {
            if (fase == Fase.Apagado && enfriamientoRestante <= 0f)
                Activar();
        }
        else
        {
            if (fase == Fase.Activo)
                Desactivar();
        }
    }

    void Update()
    {
        if (enfriamientoRestante > 0f) enfriamientoRestante -= Time.deltaTime;
        if (tiempoImpacto > 0f) tiempoImpacto -= Time.deltaTime;
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
        MostrarEscudo(true);
        animador.SetBool("IsActive", true);
        PausarRegeneracion(true);
    }

    void Desactivar()
    {
        // Energía agotada → sobrecarga; si el jugador soltó, desactivación normal
        if (estadisticas != null && estadisticas.energia <= 0)
            Sobrecargar();
        else
            DesactivarNormal();
    }

    void DesactivarNormal()
    {
        fase = Fase.Desactivando;
        animador.SetBool("IsActive", false);
        colisionador.enabled = false;
        PausarRegeneracion(false);   // ya no consume, que regenere
    }

    void Sobrecargar()
    {
        fase = Fase.Destruyendo;
        enfriamientoRestante = enfriamientoSobrecarga;
        animador.SetBool("IsActive", false);
        animador.SetTrigger("Destroy");
        colisionador.enabled = false;
        // Sigue pausada durante la animación de destrucción
    }

    // Llamado por Animation Event al final de las animaciones de apagado/destrucción
    public void TerminarEscudo()
    {
        if (fase != Fase.Desactivando && fase != Fase.Destruyendo) return;
        MostrarEscudo(false);
        fase = Fase.Apagado;
        PausarRegeneracion(false);
    }

    void PausarRegeneracion(bool pausa)
    {
        if (estadisticas != null) estadisticas.SetRegeneracionPausada(pausa);
    }

    void MostrarEscudo(bool activo) => visual.enabled = colisionador.enabled = activo;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (fase != Fase.Activo) return;
        if (!otro.CompareTag("Bullet") && !otro.CompareTag("Asteroid")) return;
        Vector2 dir = ((Vector2)otro.transform.position - (Vector2)transform.position).normalized;
        anguloImpacto = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + desfaseAnguloImpacto;
        tiempoImpacto = duracionImpacto;
        animador.SetTrigger("Impact");
        if (otro.CompareTag("Asteroid"))
        {
            var asteroide = otro.GetComponentInParent<Asteroide>();
            if (asteroide != null) asteroide.Explotar();
        }
        Destroy(otro.transform.root.gameObject);
    }
}