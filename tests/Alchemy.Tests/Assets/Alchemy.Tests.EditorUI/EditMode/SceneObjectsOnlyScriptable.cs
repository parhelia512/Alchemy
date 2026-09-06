using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI.EditMode
{
    public class SceneObjectsOnlyScriptable : ScriptableObject
    {
        [SceneObjectsOnly]
        public GameObject sceneObject;
    }
}
