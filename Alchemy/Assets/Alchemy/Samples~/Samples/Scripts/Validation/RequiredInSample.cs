using UnityEngine;
using Alchemy.Inspector;

namespace Alchemy.Samples
{
    public class RequiredInSample : MonoBehaviour
    {
        [RequiredIn(PrefabKind.InstanceInScene)]
        public GameObject Target;

        [RequiredIn(PrefabKind.PrefabAsset, "EventManager is required.")]
        public GameObject EventManager;
    }
}
