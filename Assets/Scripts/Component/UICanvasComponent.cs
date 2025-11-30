using Unity.Entities;
using UnityEngine;

namespace Component
{
    public class UICanvasComponent : IComponentData
    {
        public Canvas CanvasReference;
    }
    
    public struct MainCanvasTag : IComponentData {}
}