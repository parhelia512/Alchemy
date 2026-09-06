using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace Alchemy.Tests.EditorUI
{
    public class RequiredListLengthHost : MonoBehaviour
    {
        [RequiredListLength(1)]
        public int[] exact;

        [RequiredListLength(0, 2)]
        public List<int> range = new List<int>();

        [RequiredListLength(null, 2)]
        public int[] atMost;

        [RequiredListLength(2, null)]
        public List<string> atLeast;

        [RequiredListLength(1, Message = "Must have exactly one item.")]
        public int[] custom;

        [RequiredListLength(1)]
        public int unsupported;

        [RequiredListLength("foo", 10)]
        public int[] invalidBounds;
    }
}
