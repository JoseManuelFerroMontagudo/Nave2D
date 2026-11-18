using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Root que contiene al jugador, la cámara de juego y el HUD. " +
             "El selector lo desactiva mientras está abierto.")]
    public GameObject playerRoot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetPlayerActive(bool active)
    {
        if (playerRoot != null) playerRoot.SetActive(active);
    }
}