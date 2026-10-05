using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Stats))]
public class ShipWeapons : MonoBehaviour
{
    [Header("Cañones")]
    public int cantidadCañones = 2;
    public float anchoCañones = 2f;   // ancho total que ocupan los cañones

    [Header("Primario")]
    public GameObject prefabProyectilPrimario;
    public float cadenciaPrimario = 10f;
    public float velocidadProyectilPrimario = 20f;

    [Header("Secundario (misil)")]
    public GameObject prefabProyectilSecundario;
    public float cadenciaSecundario = 2f;
    public float velocidadProyectilSecundario = 12f;
    public int costoMunicionSecundario = 1;

    readonly List<Transform> cañones = new();
    float siguienteDisparoPrimario;
    float siguienteDisparoSecundario;
    bool primarioMantenido;

    Stats estadisticas;

    void Awake()
    {
        estadisticas = GetComponent<Stats>();
        ConstruirCañones();
    }

    void ConstruirCañones()
    {
        cañones.Clear();
        int cantidad = Mathf.Max(1, cantidadCañones);
        float separacion = anchoCañones / cantidad;
        float centro = (cantidad - 1) * 0.5f;

        for (int i = 0; i < cantidad; i++)
        {
            var cañon = new GameObject($"Cañon_{i}").transform;
            cañon.SetParent(transform, false);
            cañon.localPosition = Vector3.right * (i - centro) * separacion;
            cañones.Add(cañon);
        }
    }

    // ==================== INPUT ====================

    public void OnPrimaryAttack(InputValue valor) => primarioMantenido = valor.isPressed;

    public void OnSecondaryAttack()
    {
        if (Time.time < siguienteDisparoSecundario) return;
        if (prefabProyectilSecundario == null) return;
        if (estadisticas == null || !estadisticas.ConsumirMunicion(costoMunicionSecundario)) return;

        siguienteDisparoSecundario = Time.time + 1f / cadenciaSecundario;
        Disparar(prefabProyectilSecundario, velocidadProyectilSecundario, transform);
    }

    void Update()
    {
        // Disparo continuo mientras se mantiene el primario
        if (!primarioMantenido || Time.time < siguienteDisparoPrimario) return;

        siguienteDisparoPrimario = Time.time + 1f / cadenciaPrimario;
        foreach (var cañon in cañones)
            Disparar(prefabProyectilPrimario, velocidadProyectilPrimario, cañon);
    }

    // ==================== DISPARO ====================

    void Disparar(GameObject prefab, float velocidad, Transform origen)
    {
        if (prefab == null) return;

        var go = Instantiate(prefab, origen.position, origen.rotation);
        if (go.TryGetComponent<Projectile>(out var p))
            p.Initialize(origen.up * velocidad);
    }
}