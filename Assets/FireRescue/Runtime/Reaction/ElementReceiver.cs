using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Narrow reaction boundary for scene objects that accept an element amount.
    /// </summary>
    public abstract class ElementReceiver : MonoBehaviour
    {
        public abstract void Receive(ElementKind element, float amount);
    }
}
