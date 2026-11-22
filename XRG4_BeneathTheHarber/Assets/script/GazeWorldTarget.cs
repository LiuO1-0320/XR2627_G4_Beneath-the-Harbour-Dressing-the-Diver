using System;
using UnityEngine;

/// <summary>A head-gaze action on a collider or the parent of a group of colliders.</summary>
[DisallowMultipleComponent]
public sealed class GazeWorldTarget : MonoBehaviour
{
    Func<bool> available;
    Action activate;

    public void Bind(Func<bool> available, Action activate)
    {
        this.available = available;
        this.activate = activate;
    }

    internal bool Available => isActiveAndEnabled && activate != null && available != null && available();
    internal void Activate() => activate();
}
