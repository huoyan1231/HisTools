using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Logger = HisTools.Utils.Logger;

namespace HisTools.Prefabs
{
    public sealed class PrefabDatabase
    {
        private static PrefabDatabase _instance;
        public static PrefabDatabase Instance => _instance ??= new PrefabDatabase();

        private readonly Dictionary<string, AssetBundle> _loadedBundles = new();
        private readonly Dictionary<string, Object> _loadedAssets = new();

        [CanBeNull]
        public GameObject GetObject(string name, bool active)
        {
            var go = GetCached<GameObject>(name);

            if (!go)
            {
                // name format: "bundleName/assetName"
                var slashIndex = name.IndexOf('/');
                if (slashIndex > 0)
                {
                    var bundleName = name[..slashIndex];
                    var assetName = name[(slashIndex + 1)..];
                    LoadAsset<GameObject>(bundleName, assetName);
                    go = GetCached<GameObject>(name);
                }
            }

            if (!go)
            {
                Logger.Error($"PrefabDatabase: Prefab '{name}' not found");
                return null;
            }

            go.SetActive(active);
            return go;
        }

        [CanBeNull]
        public AssetBundle LoadBundle(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out var cached))
                return cached;

            var bundlePath = ResolveBundlePath(bundleName);

            if (!File.Exists(bundlePath))
            {
                Logger.Error($"PrefabDatabase: Bundle '{bundleName}' not found");
                return null;
            }

            return LoadBundleFromFile(bundleName, bundlePath);
        }

        [CanBeNull]
        private AssetBundle LoadBundleFromFile(string name, string path)
        {
            var bundle = AssetBundle.LoadFromFile(path);
            if (!bundle) return null;

            _loadedBundles[name] = bundle;
            return bundle;
        }

        private static string ResolveBundlePath(string bundleName)
        {
            var primary = Path.Combine(Constants.Paths.PluginDllDir, "Assets", bundleName);
            return File.Exists(primary) ? primary : Path.Combine(Constants.Paths.PluginDllDir, bundleName);
        }

        public void LoadAsset<T>(string bundleName, string assetName) where T : Object
        {
            var cacheKey = $"{bundleName}/{assetName}";

            if (_loadedAssets.ContainsKey(cacheKey))
                return;

            var bundle = LoadBundle(bundleName);

            if (!bundle)
            {
                Logger.Error($"PrefabDatabase: Bundle '{bundleName}' not found");
                return;
            }
            
            var asset = bundle.LoadAsset<T>(assetName);
            if (!asset)
            {
                Logger.Error($"PrefabDatabase: Asset '{assetName}' not found in '{bundleName}'");
                return;
            }
            
            Cache(cacheKey, asset);
        }

        [CanBeNull]
        private T GetCached<T>(string key) where T : Object
        {
            if (_loadedAssets.TryGetValue(key, out var obj) && obj != null && obj is T typed)
            {
                return typed;
            }

            return null;
        }

        private T Cache<T>(string key, T asset) where T : Object
        {
            _loadedAssets[key] = asset;
            return asset;
        }
    }
}