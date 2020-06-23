using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Serialization;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace threeDtbd.Workflow.PackageManagement
{
    public class AssetDescriptor : ScriptableObject
    {
        public string documentation;
        public string unityForum;
        public enum PackageType {  LocalAsset, Git, Package }
        public PackageType packageType;
        public string id;
        [SerializeField, FormerlySerializedAs("unityPackagePath")]
        private string m_UnityPackagePath;
        public string gitURI;
        public string scriptDefines;
        public bool _isLocalPackageInstalled = false;

        public virtual string unityPackagePath
        {
            get { return m_UnityPackagePath; }
            set { m_UnityPackagePath = value; }
        }

        public bool isInstalled
        {
            get 
            {  
                if (IsLocalAsset)
                {
                    return _isLocalPackageInstalled;
                } else if (IsGitPackage)
                {
                    return VersionControl.Git.IsRemotePresent(this);
                } else
                {
                    List<string> matchingPackages = AssetDatabase.FindAssets("package")
                            .Select(AssetDatabase.GUIDToAssetPath).Where(x => x.Contains(unityPackagePath)).ToList();
                    if (matchingPackages.Count > 1)
                    {
                        Debug.LogWarning("More than one package containing the asset path " + unityPackagePath + ". This is not accounted for in code. Not expecting problems but... be aware.");
                    }
                    return matchingPackages.Count > 0;
                }
            }
            set { _isLocalPackageInstalled = value; }
        }

        public bool IsLocalAsset
        {
            get { 
                return string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(unityPackagePath); 
            }
        }
        public bool IsGitPackage
        {
            get
            {
                return string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(gitURI);
            }
        }

        virtual internal void OnGUI()
        {
        }

        internal void Install()
        {
            // TODO installing can take a long time, need UI feedback
            if (IsLocalAsset)
            {
                _isLocalPackageInstalled = true;
                AssetDatabase.ImportPackage(unityPackagePath, false);
            }
            else if (IsGitPackage)
            {   
                // TODO the path should be configurable
                VersionControl.Git.AddAsSubmodule(this, "3dtbd");
            }
            else
            {
                Client.Add(id);
            }
        }

        internal static AssetDescriptor CreateInstanceFromGitHub(string uri)
        {
            AssetDescriptor desc = ScriptableObject.CreateInstance<AssetDescriptor>();
            int start = uri.IndexOf(':') + 1;
            desc.name = uri.Substring(start, uri.Length - ".git".Length - start).Replace("/","_");
            desc.gitURI = uri;
            desc.packageType = AssetDescriptor.PackageType.Git;

            return desc;
        }

        internal static AssetDescriptor CreateInstanceFromPackageInfo(PackageInfo info)
        {
            AssetDescriptor desc = ScriptableObject.CreateInstance<AssetDescriptor>();
            desc.name = info.displayName;
            desc.id = info.packageId;
            desc.unityPackagePath = info.assetPath;
            desc.packageType = AssetDescriptor.PackageType.Package;

            return desc;
        }

        internal static AssetDescriptor CreateInstanceFromLocalPackage(string filepath)
        {
            Type t = GetTypeFor(filepath);

            AssetDescriptor desc = ScriptableObject.CreateInstance(t.FullName) as AssetDescriptor;
            if (t == typeof(AssetDescriptor))
            {
                desc.unityPackagePath = filepath;
            }
            desc.name = Path.GetFileNameWithoutExtension(filepath);
            desc.packageType = AssetDescriptor.PackageType.LocalAsset;

            return desc;
        }

        /// <summary>
        /// Returns a Type, always of or descended from AssetDescriptor, that will represent this the asset at the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Type GetTypeFor(string path)
        {
            // TODO cache results for performance
            Type[] types = typeof(AssetDescriptor).Assembly.GetTypes();
            foreach (Type t in types)
            {
                AssetDescriptor instance = null;
                if (t.IsSubclassOf(typeof(AssetDescriptor)))
                {
                    instance = ScriptableObject.CreateInstance(t.FullName) as AssetDescriptor;
                    PropertyInfo pathProperty = t.GetProperty("unityPackagePath");
                    if (path.EndsWith((string)t.GetProperty("unityPackagePath").GetValue(instance)))
                    {
                        ScriptableObject.DestroyImmediate(instance);
                        return t;
                    }
                }

                if (instance != null)
                {
                    ScriptableObject.DestroyImmediate(instance);
                }
            }

            return typeof(AssetDescriptor);
        }
    }
}