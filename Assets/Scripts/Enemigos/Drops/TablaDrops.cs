using UnityEngine;

[System.Serializable]
public class EntradaDrop
{
    public GameObject prefab;
    [Min(0)] public int cantidadMin = 0;
    [Min(0)] public int cantidadMax = 1;
}

[CreateAssetMenu(menuName = "Drops/TablaDrops", fileName = "TablaDrops")]
public class TablaDrops : ScriptableObject
{
    public EntradaDrop[] entradas;

    [Header("Dispersión (efecto explosión)")]
    public float fuerzaMin = 2f;
    public float fuerzaMax = 5f;
}