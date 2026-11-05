using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ControladorAnomalia : MonoBehaviour
{
    [Header("Sistema de Vida / Salud")]
    public float vidaMaxima = 100f;
    private float vidaActual;

    [Header("Referencias de Sprites")]
    public GameObject spriteAgujeroNegro;
    public GameObject spriteAgujeroBlanco;

    [Header("Efecto de Luz Cegadora (UI)")]
    public Image imagenFlashUI; // El objeto 'EfectoFlash'
    public ParticleSystem FXExplosion;

    [Header("Tiempos de la Transición")]
    public float velocidadImplosion = 5f;
    public float duracionCeguera = 0.15f;
    public float velocidadDesvanecimientoFlash = 2f;

    private bool enFaseAgujeroBlanco = false;
    private bool cambiandoDeFase = false;

    private void Start()
    {
        vidaActual = vidaMaxima;

        if (spriteAgujeroNegro != null) spriteAgujeroNegro.SetActive(true);
        if (spriteAgujeroBlanco != null) spriteAgujeroBlanco.SetActive(false);
        if (FXExplosion != null) FXExplosion.Stop();

        if (imagenFlashUI != null)
        {
            Color c = imagenFlashUI.color;
            c.a = 0f;
            imagenFlashUI.color = c;
        }
    }

    // Método público para que las balas de la nave le quiten vida
    public void RecibirDano(float cantidadDano)
    {
        if (enFaseAgujeroBlanco || cambiandoDeFase) return;

        vidaActual -= cantidadDano;
        Debug.Log($"[Agujero Negro] Vida restante: {vidaActual}/{vidaMaxima}");

        if (vidaActual <= 0f)
        {
            DerrotarAgujeroNegro();
        }
    }

    private void DerrotarAgujeroNegro()
    {
        if (!cambiandoDeFase)
        {
            StartCoroutine(RutinaTransicionCegadora());
        }
    }

    private IEnumerator RutinaTransicionCegadora()
    {
        cambiandoDeFase = true;

        Vector3 escalaInicial = transform.localScale;
        float t = 0f;

        // 1. IMPLOSIÓN
        while (t < 1f)
        {
            t += Time.deltaTime * velocidadImplosion;
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.one * 0.01f, t);

            if (imagenFlashUI != null)
            {
                Color c = imagenFlashUI.color;
                c.a = Mathf.Lerp(0f, 0.4f, t);
                imagenFlashUI.color = c;
            }
            yield return null;
        }

        // 2. DETONACIÓN Y TRANSICIÓN
        if (imagenFlashUI != null)
        {
            Color c = imagenFlashUI.color;
            c.a = 1f;
            imagenFlashUI.color = c;
        }

        // Detener la generación de asteroides
        SpawnerAnomalia spawner = GetComponentInChildren<SpawnerAnomalia>();
        if (spawner != null)
        {
            spawner.DetenerGeneracion();
        }

        if (spriteAgujeroNegro != null) spriteAgujeroNegro.SetActive(false);
        if (spriteAgujeroBlanco != null) spriteAgujeroBlanco.SetActive(true);
        if (FXExplosion != null) FXExplosion.Play();

        enFaseAgujeroBlanco = true;
        transform.localScale = escalaInicial * 1.3f;

        yield return new WaitForSeconds(duracionCeguera);

        // 3. DESVANECIMIENTO DE LUZ
        float tiempoFlash = 0f;
        while (tiempoFlash < 1f)
        {
            tiempoFlash += Time.deltaTime * velocidadDesvanecimientoFlash;

            if (imagenFlashUI != null)
            {
                Color c = imagenFlashUI.color;
                c.a = Mathf.Lerp(1f, 0f, tiempoFlash);
                imagenFlashUI.color = c;
            }

            transform.localScale = Vector3.Lerp(escalaInicial * 1.3f, escalaInicial, tiempoFlash);
            yield return null;
        }

        cambiandoDeFase = false;
    }

    // Detectar colisión/triggger por si las balas no usan script de daño directo
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Attack1") || collision.CompareTag("Attack2"))
        {
            RecibirDano(10f); // Le resta 10 de vida por cada bala (Ajustable)
            Destroy(collision.gameObject); // Destruye la bala al impactar
        }
    }
}