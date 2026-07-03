using UnityEngine;

[DisallowMultipleComponent]
public sealed class Grabbable : MonoBehaviour
{
    [Tooltip("Used by hands to decide whether a surface is intentionally grabbable.")]
    public bool allowGrip = true;
}
