using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class Stats : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vida;

    [Header("Energía (misiles, ataques secundarios)")]
    public int energiaMaxima = 100;
    public int energia;
    public int regeneracionEnergia = 10;   // puntos por segundo

    [Header("Munición (solo acciones especiales: bombas, bullet hell, etc.)")]
    public int municionMaxima = 3;
    public int municion;

    [Header("Calor (evita fuego continuo)")]
    public float calorMaximo = 100f;
    public float calorActual;
    public float disipacionPorSegundo = 35f;
    [Tooltip("Segundos tras el último disparo antes de empezar a enfriar")]
    public float retrasoEnfriamiento = 0.4f;
    [Range(0f, 1f)]
    [Tooltip("Baja a este % del máximo para salir del estado recalentado")]
    public float umbralSalidaRecalentamiento = 0.5f;

    [Header("Poder (modifica cadencia y/o nº de cañones)")]
    public int poder = 1;

    [Header("Puntos")]
    public int puntos;

    Animator animador;
    PlayerInput playerInput;
    float acumuladorRegeneracion;
    float tiempoUltimoDisparo = -999f;
    bool recalentado;

    void Awake()
    {
        vida = vidaMaxima;
        energia = energiaMaxima;
        municion = municionMaxima;
        puntos = 0;
        poder = Mathf.Max(1, poder);

        animador = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;
    }

    void Update()
    {
        ActualizarCalor(Time.deltaTime);
    }

    // ---------------- VIDA ----------------
    public void RecibirDaño(int cantidad)
    {
        if (cantidad <= 0) return;
        vida = Mathf.Max(0, vida - cantidad);

        if (vida <= 0)
        {
            playerInput.enabled = false;
            animador.SetTrigger("Death");
        }
    }

    public void Curar(int cantidad)
    {
        if (cantidad <= 0) return;
        vida = Mathf.Min(vidaMaxima, vida + cantidad);
    }

    public bool EstaMuerto => vida <= 0;

    // ---------------- ENERGÍA ----------------
    public int DrenarEnergia(int cantidad)
    {
        if (cantidad <= 0) return 0;
        int usado = Mathf.Min(energia, cantidad);
        energia -= usado;
        return usado;
    }

    public void RegenerarEnergia(float deltaTime)
    {
        if (energia >= energiaMaxima)
        {
            acumuladorRegeneracion = 0f;
            return;
        }

        acumuladorRegeneracion += regeneracionEnergia * deltaTime;
        int entero = Mathf.FloorToInt(acumuladorRegeneracion);
        if (entero > 0)
        {
            energia = Mathf.Min(energiaMaxima, energia + entero);
            acumuladorRegeneracion -= entero;
        }
    }

    public void AñadirEnergia(int cantidad)
    {
        if (cantidad <= 0) return;
        energia = Mathf.Min(energiaMaxima, energia + cantidad);
    }

    public bool TieneEnergia => energia > 0;

    // ---------------- MUNICIÓN ----------------
    public bool ConsumirMunicion(int cantidad = 1)
    {
        if (cantidad <= 0) return true;
        if (municion < cantidad) return false;
        municion -= cantidad;
        return true;
    }

    public void AñadirMunicion(int cantidad)
    {
        if (cantidad <= 0) return;
        municion = Mathf.Min(municionMaxima, municion + cantidad);
    }

    // ---------------- CALOR ----------------
    public bool Recalentado => recalentado;

    /// <summary>0..1 para barras de UI.</summary>
    public float CalorNormalizado => calorMaximo > 0f ? calorActual / calorMaximo : 0f;

    public void AñadirCalor(float cantidad)
    {
        if (cantidad <= 0f) return;
        calorActual = Mathf.Min(calorMaximo, calorActual + cantidad);
        tiempoUltimoDisparo = Time.time;
        if (calorActual >= calorMaximo) recalentado = true;
    }

    public void ActualizarCalor(float deltaTime)
    {
        if (calorActual <= 0f) return;
        if (Time.time - tiempoUltimoDisparo < retrasoEnfriamiento) return;

        calorActual = Mathf.Max(0f, calorActual - disipacionPorSegundo * deltaTime);

        if (recalentado && calorActual <= calorMaximo * umbralSalidaRecalentamiento)
            recalentado = false;
    }

    // ---------------- PODER ----------------
    public void AñadirPoder(int cantidad)
    {
        poder = Mathf.Max(1, poder + cantidad);
    }

    // ---------------- PUNTOS ----------------
    public void AñadirPuntos(int cantidad)
    {
        if (cantidad <= 0) return;
        puntos += cantidad;
    }

    public void Morir()
    {
        SceneLoader.Instance.LoadSingle("GameOver");
    }
}