using System;
using UnityEngine;

namespace ZombieSurvivor
{
    // Shared logical coordinates keep prototype controls readable on high-density phones.
    public readonly struct PrototypeGUI : IDisposable
    {
        private readonly Matrix4x4 previous;
        private static Rect Safe => Screen.safeArea.width > 0 ? Screen.safeArea : new Rect(0, 0, Screen.width, Screen.height);
        private static float Scale => Safe.width / 480f;
        public static float Width => 480;
        public static float Height => Safe.height / Scale;
        public PrototypeGUI(bool apply = true)
        {
            previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Safe.x, Screen.height - Safe.yMax, 0), Quaternion.identity, new Vector3(Scale, Scale, 1));
        }
        public static Vector2 FromScreen(Vector2 point) => new Vector2((point.x - Safe.x) / Scale, (Safe.yMax - point.y) / Scale);
        public void Dispose() => GUI.matrix = previous;
    }
}
