using UnityEngine;
using UnityEditor;

/// <summary>
/// Nur für den Editor: trägt eine Beschreibung, die im Inspector erscheint.
/// </summary>
[DisallowMultipleComponent]          // verhindert doppelte Anbringung
public class DevDescription : MonoBehaviour
{
    [TextArea(2, 5)]                 // mehrzeiliges Eingabefeld
    [SerializeField] private string description = "Erklärtext hier …";

    // Falls andere Tools zugreifen wollen
    public string Description => description;
}


