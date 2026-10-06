using System;
using UnityEngine;

public class DetectorMuerteEnemigo : MonoBehaviour
{
    public event Action OnMuerte;
    private bool avisado = false;

    void OnDestroy()
    {
        if (!avisado)
        {
            avisado = true;
            OnMuerte?.Invoke();
        }
    }
}