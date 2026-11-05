using UnityEngine;
using System.Collections;

public abstract class PatronBase : ScriptableObject
{
    public abstract IEnumerator Ejecutar(ContextoPatron ctx);
}