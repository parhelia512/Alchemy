using Alchemy.Inspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Alchemy.Editor
{
    internal static class PrefabKindUtility
    {
        public static PrefabKind GetPrefabKind(UnityEngine.Object target)
        {
            if (target == null) return PrefabKind.None;

            var gameObject = GetGameObject(target);
            if (gameObject == null) return PrefabKind.None;

            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                if (TryGetPreviewPrefabAssetPath(gameObject, out var previewAssetPath))
                {
                    return IsNestedInstanceInPreview(gameObject, previewAssetPath)
                        ? PrefabKind.InstanceInPrefab
                        : GetAssetKindFromPath(previewAssetPath);
                }

                if (IsPersistentVariantAsset(gameObject))
                {
                    return PrefabKind.Variant;
                }

                return IsInstanceInPrefab(gameObject)
                    ? PrefabKind.InstanceInPrefab
                    : PrefabKind.InstanceInScene;
            }

            if (EditorUtility.IsPersistent(gameObject) || PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return GetAssetKind(gameObject);
            }

            if (TryGetPreviewPrefabAssetPath(gameObject, out var assetPath))
            {
                return GetAssetKindFromPath(assetPath);
            }

            if (IsPrefabPreviewObject(gameObject))
            {
                return GetPreviewAssetKind(gameObject);
            }

            return PrefabKind.NonPrefabInstance;
        }

        static bool IsPersistentVariantAsset(GameObject gameObject)
        {
            if (!PrefabUtility.IsPartOfVariantPrefab(gameObject))
            {
                return false;
            }

            if (!EditorUtility.IsPersistent(gameObject) && !PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return false;
            }

            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            var outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            return nearest == null || outermost == null || nearest == outermost;
        }

        static bool IsNestedInstanceInPreview(GameObject gameObject, string previewAssetPath)
        {
            var nearestPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (string.IsNullOrEmpty(nearestPath))
            {
                return true;
            }

            if (nearestPath == previewAssetPath)
            {
                return false;
            }

            var previewAsset = AssetDatabase.LoadAssetAtPath<GameObject>(previewAssetPath);
            if (previewAsset == null)
            {
                return true;
            }

            var baseAsset = PrefabUtility.GetCorrespondingObjectFromSource(previewAsset);
            return baseAsset == null || AssetDatabase.GetAssetPath(baseAsset) != nearestPath;
        }

        static bool IsInstanceInPrefab(GameObject gameObject)
        {
            var nearest = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);
            var outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            if (nearest != null && outermost != null && nearest != outermost)
            {
                return true;
            }

            if (EditorUtility.IsPersistent(gameObject) || PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return true;
            }

            return IsPrefabPreviewObject(gameObject);
        }

        static bool IsPrefabPreviewObject(GameObject gameObject)
        {
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
            {
                return true;
            }

            return gameObject.scene.IsValid() && EditorSceneManager.IsPreviewScene(gameObject.scene);
        }

        static bool TryGetPreviewPrefabAssetPath(GameObject gameObject, out string assetPath)
        {
            assetPath = null;

            var stage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (stage != null && !string.IsNullOrEmpty(stage.assetPath))
            {
                assetPath = stage.assetPath;
                return true;
            }

            if (!gameObject.scene.IsValid() || !EditorSceneManager.IsPreviewScene(gameObject.scene))
            {
                return false;
            }

            var scenePath = gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                return false;
            }

            if (AssetDatabase.LoadMainAssetAtPath(scenePath) != null)
            {
                assetPath = scenePath;
                return true;
            }

            return false;
        }

        static PrefabKind GetPreviewAssetKind(GameObject gameObject)
        {
            if (PrefabUtility.IsPartOfVariantPrefab(gameObject)) return PrefabKind.Variant;
            if (PrefabUtility.IsPartOfRegularPrefab(gameObject)) return PrefabKind.Regular;
            return PrefabKind.None;
        }

        static PrefabKind GetAssetKindFromPath(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return asset == null ? PrefabKind.None : GetAssetKind(asset);
        }

        static PrefabKind GetAssetKind(UnityEngine.Object target)
        {
            return PrefabUtility.GetPrefabAssetType(target) switch
            {
                PrefabAssetType.Regular => PrefabKind.Regular,
                PrefabAssetType.Variant => PrefabKind.Variant,
                _ => PrefabKind.None,
            };
        }

        static GameObject GetGameObject(UnityEngine.Object target)
        {
            switch (target)
            {
                case GameObject gameObject:
                    return gameObject;
                case Component component:
                    return component.gameObject;
                default:
                    return null;
            }
        }
    }
}
