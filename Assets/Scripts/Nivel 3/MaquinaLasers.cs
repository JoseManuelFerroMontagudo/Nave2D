using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaquinaLasers : MonoBehaviour
{
    [Header("Configuración de Rayos")]
    [Range(1, 12)]
    public int cantidadRayos = 4;             // Ej: 4 rayos forman una cruz giratoria
    public float longitudRayos = 25f;
    public float velocidadRotacion = 45f;     // Grados por segundo (positivo = antihorario, negativo = horario)

    [Header("Tiempos de Ataque")]
    public float tiempoAdvertencia = 1.5f;    // Tiempo de aviso antes de hacer daño
    public float duracionDisparo = 5f;        // Cuánto tiempo dura girando el láser activo

    [Header("Efectos Visuales (VFX)")]
    public GameObject prefabImpactoLaser;

    [Header("Apariencia y Materiales")]
    public Material materialLaser;            // Usa aquí un material del pack LineRendererShader
    public string sortingLayerName = "Elementos_Juego";
    public float grosorAdvertencia = 0.12f;
    public float grosorDisparo = 0.8f;
    public Color colorAdvertencia = new Color(1f, 0.9f, 0.2f, 0.45f);
    public Color colorDisparo = new Color(0.2f, 0.9f, 1f, 1f); // Cian brillante (ideal para Agujero Blanco) o Rojo

    [Header("Daño al Jugador")]
    public int danoPorImpacto = 15;
    public float intervaloEntreGolpes = 0.4f; // Para no matar al jugador en 0.05s si lo roza

    private List<LineRenderer> rayos = new List<LineRenderer>();
    private bool maquinaActiva = false;
    private bool haciendoDano = false;
    private float timerInmunidadJugador = 0f;

    private void Awake()
    {
        GenerarLineRenderers();
    }

    private void GenerarLineRenderers()
    {
        // Limpiar si había anteriores
        foreach (var lr in rayos)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        rayos.Clear();

        for (int i = 0; i < cantidadRayos; i++)
        {
            GameObject objRayo = new GameObject("RayoMaquina_" + i);
            objRayo.transform.SetParent(transform);
            objRayo.transform.localPosition = Vector3.zero;

            LineRenderer lr = objRayo.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.sortingLayerName = sortingLayerName;

            if (materialLaser != null)
            {
                lr.material = materialLaser;
            }
            else
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }

            lr.enabled = false;
            rayos.Add(lr);
        }
    }

    public void ActivarMaquina()
    {
        if (!maquinaActiva)
        {
            // Si cambiaste la cantidad de rayos en el Inspector en tiempo real, regenera
            if (rayos.Count != cantidadRayos) GenerarLineRenderers();
            StartCoroutine(RutinaMaquinaLasers());
        }
    }

    public void DetenerMaquina()
    {
        StopAllCoroutines();
        maquinaActiva = false;
        haciendoDano = false;
        foreach (var lr in rayos)
        {
            if (lr != null) lr.enabled = false;
        }
    }

    private IEnumerator RutinaMaquinaLasers()
    {
        maquinaActiva = true;
        haciendoDano = false;

        // 1. FASE DE ADVERTENCIA (Rayos finos girando suavemente sin hacer daño)
        ConfigurarVisualRayos(grosorAdvertencia, colorAdvertencia, true);

        float t = 0f;
        while (t < tiempoAdvertencia)
        {
            t += Time.deltaTime;
            transform.Rotate(0f, 0f, velocidadRotacion * 0.5f * Time.deltaTime); // Gira a mitad de velocidad al avisar
            ActualizarPosicionesRayos();
            yield return null;
        }

        // 2. FASE DE DISPARO LETAL (Rayos gruesos + Daño)
        ConfigurarVisualRayos(grosorDisparo, colorDisparo, true);
        haciendoDano = true;
        timerInmunidadJugador = 0f;

        float tiempoActivo = 0f;
        while (tiempoActivo < duracionDisparo)
        {
            tiempoActivo += Time.deltaTime;
            if (timerInmunidadJugador > 0f) timerInmunidadJugador -= Time.deltaTime;

            transform.Rotate(0f, 0f, velocidadRotacion * Time.deltaTime);
            ActualizarPosicionesRayos();
            DetectarColisiones();

            yield return null;
        }

        // 3. APAGADO
        DetenerMaquina();
    }

    private void ConfigurarVisualRayos(float grosor, Color color, bool encendido)
    {
        foreach (var lr in rayos)
        {
            lr.enabled = encendido;
            lr.startWidth = grosor;
            lr.endWidth = grosor;
            lr.startColor = color;
            lr.endColor = color;
        }
    }

    public void ConfigurarDificultad(int nuevaCantidadRayos, float nuevaVelocidad)
    {
        cantidadRayos = nuevaCantidadRayos;
        velocidadRotacion = nuevaVelocidad;
        GenerarLineRenderers();
    }

    private void ActualizarPosicionesRayos()
    {
        float pasoAngular = 360f / rayos.Count;
        Vector3 origen = transform.position;

        for (int i = 0; i < rayos.Count; i++)
        {
            float anguloActual = transform.eulerAngles.z + (pasoAngular * i);
            Vector3 direccion = Quaternion.Euler(0f, 0f, anguloActual) * Vector3.right;
            Vector3 destino = origen + (direccion * longitudRayos);

            rayos[i].SetPosition(0, origen);
            rayos[i].SetPosition(1, destino);
        }
    }

    private void DetectarColisiones()
    {
        if (!haciendoDano || timerInmunidadJugador > 0f) return;

        float pasoAngular = 360f / rayos.Count;
        Vector3 origen = transform.position;

        for (int i = 0; i < rayos.Count; i++)
        {
            float anguloActual = transform.eulerAngles.z + (pasoAngular * i);
            Vector3 direccion = Quaternion.Euler(0f, 0f, anguloActual) * Vector3.right;

            RaycastHit2D[] impactos = Physics2D.CircleCastAll(origen, grosorDisparo * 0.35f, direccion, longitudRayos);

            foreach (RaycastHit2D hit in impactos)
            {
                if (hit.collider != null && (hit.collider.CompareTag("Player") || hit.transform.root.CompareTag("Player")))
                {
                    hit.transform.root.BroadcastMessage("RecibirDaño", danoPorImpacto, SendMessageOptions.DontRequireReceiver);
                    timerInmunidadJugador = intervaloEntreGolpes;
                    // Instanciar efecto de impacto en el punto exacto donde tocó la nave
                    if (prefabImpactoLaser != null)
                    {
                        GameObject fx = Instantiate(prefabImpactoLaser, hit.point, Quaternion.identity);
                        Destroy(fx, 1.5f); // Lo destruye automáticamente tras 1.5 segundos para no llenar la escena
                    }
                    return; // Sale tras aplicar el golpe para respetar el tiempo de inmunidad
                }
            }
        }
    }

    public bool EstaActiva()
    {
        return maquinaActiva;
    }
}