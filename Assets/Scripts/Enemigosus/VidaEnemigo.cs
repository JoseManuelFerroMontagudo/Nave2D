using UnityEngine;

public class VidaEnemigo : MonoBehaviour
{
    [Header("Vida")]
    public float vida = 100f;

    [Header("Daño por tag")]
    public string tagDañoDebil = "Attack1";   // 1 de daño
    public string tagDañoFuerte = "Attack2";  // 5 de daño

    [Header("Muerte")]
    public bool destruirAlMorir = true;

    private bool muerto = false;

    void OnTriggerEnter2D(Collider2D otro)

    {
        if (GetComponent<ControladorEnemyNv1>() != null) return;

        Debug.Log($"{name} detectó trigger con {otro.name} (tag: {otro.tag})");
        if (muerto) return;

        // Solo nos interesan proyectiles con tags Attack1 / Attack2
        string tag = otro.tag;

        float daño = 0f;
        if (tag == tagDañoDebil) daño = 1f;
        else if (tag == tagDañoFuerte) daño = 50f;

        if (daño > 0f)
        {
            Debug.Log($"Voy a aplicar {daño} de daño a {name}");
            AplicarDaño(daño);
            Destroy(otro.gameObject);
        }
        else
        {
            string bytesRecibido = "";
            foreach (char c in tag) bytesRecibido += ((int)c).ToString() + " ";

            string bytesDebil = "";
            foreach (char c in tagDañoDebil) bytesDebil += ((int)c).ToString() + " ";

            string bytesFuerte = "";
            foreach (char c in tagDañoFuerte) bytesFuerte += ((int)c).ToString() + " ";

            Debug.Log($"RECIBIDO: [{tag}] bytes: {bytesRecibido}\n" +
                      $"DEBIL:    [{tagDañoDebil}] bytes: {bytesDebil}\n" +
                      $"FUERTE:   [{tagDañoFuerte}] bytes: {bytesFuerte}");
        }
    }

    public void AplicarDaño(float daño)
    {
        if (muerto) return;

        vida -= daño;
        Debug.Log($"{name} recibió {daño} de daño. Vida restante: {vida}");

        if (vida <= 0f)
            Morir();
    }

    void Morir()
    {
        muerto = true;
        Debug.Log($"{name} ha muerto.");

        if (destruirAlMorir)
            Destroy(gameObject);
    }
}