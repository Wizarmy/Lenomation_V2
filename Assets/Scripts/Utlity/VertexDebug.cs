using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class VertexDebug
{
    /// <summary>
    /// Draws a small circle at each vertex in the list.
    /// Call from OnDrawGizmos / OnDrawGizmosSelected.
    /// </summary>
    public static void DrawCircles(IList<Vector3> vertices, float radius = 0.04f, Color color = default)
    {
        if (vertices == null || vertices.Count == 0) return;
        if (color == default) color = Color.cyan;

#if UNITY_EDITOR
        // Menu items / asset creators have no Handles context
        if (Event.current == null)
        {
            foreach (var v in vertices)
                Debug.Log($"VertexDebug: {v}");
            return;
        }

        Handles.color = color;
        foreach (var v in vertices)
        {
            Handles.DrawWireDisc(v, Vector3.up, radius);
            Handles.SphereHandleCap(0, v, Quaternion.identity, radius * 0.6f, EventType.Repaint);
        }
#else
    Gizmos.color = color;
    foreach (var v in vertices)
        Gizmos.DrawWireSphere(v, radius);
#endif
    }
}