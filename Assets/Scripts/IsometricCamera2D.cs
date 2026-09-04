using UnityEngine;

public class IsometricCamera2D : MonoBehaviour
{
    [Header("Órbita y Encuadre")]
    public Transform target;
    public float distance = 5.5f;
    public float height = 3.5f;
    public float lookAtOffsetY = 2.5f;

    [Header("Velocidad de Seguimiento")]
    public float normalSpeed = 8f;
    public float uTurnCamSpeed = 5f;

    private float currentAngleY;
    private float targetAngleY;
    private float currentSpeed;
    private bool isUTurning = false;

    void Start()
    {
        if (target != null)
        {
            currentAngleY = target.eulerAngles.z + 180f; // En 2D, la rotación está en Z
            targetAngleY = currentAngleY;
            currentSpeed = normalSpeed;
        }
        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = 45f;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // El objetivo es estar 180° detrás de la nave (en el plano XY)
        targetAngleY = target.eulerAngles.z + 180f;

        if (isUTurning) currentSpeed = uTurnCamSpeed;
        else currentSpeed = normalSpeed;

        currentAngleY = Mathf.LerpAngle(currentAngleY, targetAngleY, currentSpeed * Time.deltaTime);

        // Posición de la cámara en el espacio 3D (X, Y, Z)
        // La nave está en el plano XY (z=0). La cámara orbita alrededor en el plano XZ (horizontal)
        // pero la altura es en Y. Para simular una órbita en el plano XY, usamos sen/cos en X y Z.
        Vector3 direction = new Vector3(
            Mathf.Sin(currentAngleY * Mathf.Deg2Rad),
            0,
            Mathf.Cos(currentAngleY * Mathf.Deg2Rad)
        ) * distance;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y + height, 0);
        transform.position = targetPos + direction;

        // Mirar a un punto por encima de la nave para que quede abajo en pantalla
        Vector3 lookTarget = new Vector3(target.position.x, target.position.y + lookAtOffsetY, 0);
        transform.LookAt(lookTarget);

        // Forzar inclinación isométrica (X fija, Y variable)
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(35f, euler.y, 0);
    }

    public void StartUTurn()
    {
        isUTurning = true;
        Invoke(nameof(EndUTurn), 1.5f);
    }
    void EndUTurn() => isUTurning = false;
}