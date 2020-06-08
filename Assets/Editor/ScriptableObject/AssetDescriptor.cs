using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace threeDtbd.Workflow.PackageManagement
{
    public class AssetDescriptor : ScriptableObject
    {
        public enum PackageType {  LocalAsset, Git, Package }
        public PackageType packageType;
        public string id;
        public string unityPackagePath;
        public string gitURI;
        public string scriptDefines;
        public bool _isLocalPackageInstalled = false;

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
                        Debug.LogError("More than one package matching the asset path " + unityPackagePath + ". This is not accounted for in code.");
                    }
                    return matchingPackages.Count > 0;
                }
            }
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
                VersionControl.Git.AddAsRemote(this, "3dtbd");
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
    }
}