using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class RequiredInTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [RequiredIn(PrefabKind.None)]
        [SerializeField] GameObject none;

        [RequiredIn(PrefabKind.InstanceInPrefab)]
        [SerializeField] GameObject instanceInPrefab;

        [RequiredIn(PrefabKind.InstanceInScene)]
        [SerializeField] GameObject instanceInScene;

        [RequiredIn(PrefabKind.Regular)]
        [SerializeField] GameObject regular;

        [RequiredIn(PrefabKind.Variant)]
        [SerializeField] GameObject variant;

        [RequiredIn(PrefabKind.NonPrefabInstance)]
        [SerializeField] GameObject nonPrefabInstance;

        [RequiredIn(PrefabKind.PrefabInstance)]
        [SerializeField] GameObject prefabInstance;

        [RequiredIn(PrefabKind.PrefabAsset)]
        [SerializeField] GameObject prefabAsset;

        [RequiredIn(PrefabKind.PrefabInstanceAndNonPrefabInstance)]
        [SerializeField] GameObject prefabInstanceAndNonPrefabInstance;

        [RequiredIn(PrefabKind.All)]
        [SerializeField] GameObject all;

        [RequiredIn(PrefabKind.PrefabAsset, "EventManager is required.")]
        [SerializeField] GameObject eventManager;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
