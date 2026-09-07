using System;

namespace Alchemy.Inspector
{
    /// <summary>
    /// Prefab contexts used to decide when attributes such as <see cref="RequiredInAttribute"/> apply.
    /// Primitive values identify a single state; the remaining members are composite flags for matching.
    /// Classification uses Unity's current prefab connections. Scene objects lose those connections
    /// in Play Mode and are classified as <see cref="NonPrefabInstance"/>.
    /// </summary>
    [Flags]
    public enum PrefabKind
    {
        None = 0,

        /// <summary>
        /// A prefab instance nested inside another prefab.
        /// </summary>
        InstanceInPrefab = 1 << 0,

        /// <summary>
        /// A prefab instance placed in a scene.
        /// </summary>
        InstanceInScene = 1 << 1,

        /// <summary>
        /// A regular prefab asset.
        /// </summary>
        Regular = 1 << 2,

        /// <summary>
        /// A prefab variant asset.
        /// </summary>
        Variant = 1 << 3,

        /// <summary>
        /// A scene GameObject or component that is not part of a prefab instance.
        /// </summary>
        NonPrefabInstance = 1 << 4,

        /// <summary>
        /// A prefab instance in a scene or nested inside another prefab.
        /// </summary>
        PrefabInstance = InstanceInPrefab | InstanceInScene,

        /// <summary>
        /// A regular prefab asset or a prefab variant asset.
        /// </summary>
        PrefabAsset = Regular | Variant,

        /// <summary>
        /// A prefab instance or a non-prefab scene object.
        /// </summary>
        PrefabInstanceAndNonPrefabInstance =
            PrefabInstance | NonPrefabInstance,

        /// <summary>
        /// Any prefab asset, prefab instance, or non-prefab scene object.
        /// </summary>
        All = PrefabAsset | PrefabInstanceAndNonPrefabInstance,
    }
}
