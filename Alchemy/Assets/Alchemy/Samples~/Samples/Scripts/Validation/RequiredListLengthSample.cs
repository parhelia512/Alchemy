using System.Collections.Generic;
using UnityEngine;
using Alchemy.Inspector;

namespace Alchemy.Samples
{
    public class RequiredListLengthSample : MonoBehaviour
    {
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
    }
}
