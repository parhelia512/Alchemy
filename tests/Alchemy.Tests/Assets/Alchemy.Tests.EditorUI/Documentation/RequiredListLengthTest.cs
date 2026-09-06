using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    [DocumentationSample]
    public class RequiredListLengthTest : MonoBehaviour
    {
        [Order(-1)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureStart;

        #region document
        [RequiredListLength(1)]
        public int[] exact;

        [RequiredListLength(2, 4)]
        public List<int> range = new List<int> { 1 };

        [RequiredListLength(null, 2)]
        public string[] atMost = { "a", "b", "c" };

        [RequiredListLength(2, null)]
        public List<int> atLeast;

        [RequiredListLength(1, Message = "Must have exactly one item.")]
        public int[] custom;
        #endregion

        [Order(int.MaxValue)]
        [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
        [HideLabel]
        public int __docCaptureEnd;
    }
}
