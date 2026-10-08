using UnityEngine;

public class SeguirCamara3D : MonoBehaviour
{
    private Transform camaraPrincipal;

    void Start()
    {
        // Buscamos automáticamente la cámara principal del juego
        if (Camera.main != null)
        {
            camaraPrincipal = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (camaraPrincipal != null)
        {
            // El cubo se teletransporta exactamente a la coordenada de la cámara
            // eliminando la posibilidad de que la nave llegue a los bordes
            transform.position = camaraPrincipal.position;
        }
    }
}
