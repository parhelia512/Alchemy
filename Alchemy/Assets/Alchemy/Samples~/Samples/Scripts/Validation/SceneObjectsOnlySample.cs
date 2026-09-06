using UnityEngine;
using Alchemy.Inspector;

namespace Alchemy.Samples
{
    public class SceneObjectsOnlySample : MonoBehaviour
    {
        [SceneObjectsOnly]
        public GameObject sceneObject;

        [SceneObjectsOnly("Must be a scene object.")]
        public Transform sceneTransform;
    }
}
