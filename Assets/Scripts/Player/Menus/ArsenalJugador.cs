using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Armas/Arsenal Jugador")]
public class ArsenalJugador : ScriptableObject
{
    [SerializeField] private List<AtaqueBase> desbloqueados = new();
    public AtaqueBase equipadaPrimaria;
    public AtaqueBase equipadaSecundaria;

    public IReadOnlyList<AtaqueBase> Desbloqueados => desbloqueados;

    void OnEnable()
    {
        ReconciliarEquipados();
        AutoRellenarSiVacio();   // solo la primera vez, en carga del SO
    }

    // Solo sincroniza la lista con lo equipado. NO toca slots.
    public void ReconciliarEquipados()
    {
        if (desbloqueados == null) return;
        if (equipadaPrimaria != null && !desbloqueados.Contains(equipadaPrimaria)) desbloqueados.Add(equipadaPrimaria);
        if (equipadaSecundaria != null && !desbloqueados.Contains(equipadaSecundaria)) desbloqueados.Add(equipadaSecundaria);
    }

    // Solo rellena si AMBOS están vacíos (estado inicial fresco).
    // Nunca rellena si el jugador desequipó a propósito.
    public void AutoRellenarSiVacio()
    {
        if (desbloqueados == null || desbloqueados.Count == 0) return;
        if (equipadaPrimaria != null || equipadaSecundaria != null) return;

        equipadaPrimaria = desbloqueados[0];
        if (desbloqueados.Count > 1) equipadaSecundaria = desbloqueados[1];
    }

    public void Desbloquear(AtaqueBase atk)
    {
        if (atk == null) return;
        if (!desbloqueados.Contains(atk)) desbloqueados.Add(atk);
        AutoRellenarSiVacio();
    }

    public void Equipar(AtaqueBase atk, int slot)
    {
        if (atk == null) { Debug.LogWarning("[Arsenal.Equipar] atk == null"); return; }
        if (!desbloqueados.Contains(atk)) { Debug.LogWarning($"[Arsenal.Equipar] '{atk.name}' NO está en desbloqueados"); return; }

        if (slot == 0)
        {
            if (equipadaSecundaria == atk) equipadaSecundaria = equipadaPrimaria;
            equipadaPrimaria = atk;
        }
        else if (slot == 1)
        {
            if (equipadaPrimaria == atk) equipadaPrimaria = equipadaSecundaria;
            equipadaSecundaria = atk;
        }
        Debug.Log($"[Arsenal.Equipar] slot={slot} atk={atk.name} → prim={(equipadaPrimaria ? equipadaPrimaria.name : "null")} sec={(equipadaSecundaria ? equipadaSecundaria.name : "null")}");
    }

    public void Desequipar(int slot)
    {
        if (slot == 0) equipadaPrimaria = null;
        else equipadaSecundaria = null;
    }
}