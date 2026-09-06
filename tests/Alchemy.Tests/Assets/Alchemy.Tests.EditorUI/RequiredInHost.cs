using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class RequiredInHost : MonoBehaviour
    {
        [RequiredIn(PrefabKind.InstanceInScene)]
        public GameObject instanceInScene;

        [RequiredIn(PrefabKind.PrefabAsset, "EventManager is required.")]
        public GameObject prefabAsset;

        [RequiredIn(PrefabKind.All)]
        public GameObject always;
    }
}
