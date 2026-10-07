using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Animator))]
public class Stats : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vida;

    [Header("Energía")]
    public int energiaMaxima = 100;
    public int energia;
    public int regeneracionEnergia = 10;   // puntos por segundo

    [Header("Munición")]
    public int municionMaxima = 20;
    public int municion;
    Animator animador;
    float acumuladorRegeneracion;
    private PlayerInput playerInput;

    void Awake()
    {
        vida = vidaMaxima;
        energia = energiaMaxima;
        municion = municionMaxima;
        animador = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerInput.enabled = true;
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
    public void Morir()
    {
        SceneLoader.Instance.LoadSingle("GameOver");
    }
}