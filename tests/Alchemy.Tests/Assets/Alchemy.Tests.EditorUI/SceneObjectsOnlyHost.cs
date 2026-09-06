using System;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class SceneObjectsOnlyHost : MonoBehaviour
    {
        [SceneObjectsOnly]
        public GameObject sceneObject;

        [SceneObjectsOnly("Must be a scene object.")]
        public Transform sceneTransform;

        [SceneObjectsOnly]
        public GameObject[] sceneObjects;

        [SceneObjectsOnly]
        public UnityEngine.Object anyObject;

        [SceneObjectsOnly]
        public Texture2D texture;

        [SceneObjectsOnly]
        public int unsupported;

        public Nested nested;

        [Serializable]
        public class Nested
        {
            [SceneObjectsOnly]
            public GameObject sceneObject;
        }
    }
}
