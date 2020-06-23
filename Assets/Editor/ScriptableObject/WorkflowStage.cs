using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using WizardsCode.Controller;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace threeDtbd.Workflow.PackageManagement
{
    /// <summary>
    /// A workflow stage represents a singlestage in a complete project workflow.
    /// It contains a list of assets to support that stage.
    /// Each ProjectWorkflow consists of a number of stages.
    /// </summary>
    [CreateAssetMenu(fileName = "New Workflow Stage", menuName = "3D TBD/Workflow/Generic Stage")]
    public class WorkflowStage : ScriptableObject
    {   
        // TODO Why are we keeping DecriptorIDs and Descriptors. Keep just descriptors, but serialize DescriptorIDs
        public List<int> DescriptorIDs = new List<int>();
        public List<AssetDescriptor> Descriptors = new List<AssetDescriptor>();

        internal List<AssetDescriptor> installedPackagesCache = new List<AssetDescriptor>();
        internal List<AssetDescriptor> notInstalledPackagesCache = new List<AssetDescriptor>();
        internal List<AssetDescriptor> availablePackagesCache = new List<AssetDescriptor>();

        public int Count { get { return DescriptorIDs.Count; } }
        public string UninstalledCount { get { return (DescriptorIDs.Count - notInstalledPackagesCache.Count).ToString(); } }

        public string DataDirectory
        {
            get {  return EditorPrefs.GetString(WorkflowSettings.WORKFLOW_DATA_DIR_PREF_KEY); }
        }

        public int NotInstalledCount { 
            get
            {
                int count = 0;
                for (int i = Descriptors.Count - 1; i >=0; i--)
                {
                    if (!Descriptors[i].isInstalled)
                    {
                        count++;
                    }
                }
                return count;
            }
        }     

        public void Add(AssetDescriptor desc)
        {
            string path = WorkflowSettings.descriptorsDataDirectory + "/" + desc.name + ".asset";
            AssetDescriptor originalDesc = AssetDatabase.LoadAssetAtPath<AssetDescriptor>(path);
            if (originalDesc != null)
            {
                desc = originalDesc;
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(desc, path);
                AssetDatabase.SaveAssets();
            }

            if (desc.isInstalled)
            {
                installedPackagesCache.Add(desc);
            }
            else
            {
                notInstalledPackagesCache.Add(desc);
            }
            availablePackagesCache.Remove(desc);

            Descriptors.Add(desc);
            DescriptorIDs.Add(desc.GetInstanceID());

            EditorUtility.SetDirty(this);
        }

        internal bool Contains(AssetDescriptor desc)
        {
            return GetPositionOf(desc) >= 0; 
        }

        /// <summary>
        /// Return the position of the asset described in this list.
        /// </summary>
        /// <param name="desc">Position in the list or -1 if no in the list.</param>
        /// <returns></returns>
        internal int GetPositionOf(AssetDescriptor desc)
        {
            for (int i = 0; i < Descriptors.Count; i++)
            {

                if (Descriptors[i].name == desc.name)
                {
                    // TODO Check version
                    return i;
                }
                else if (Descriptors[i].name == desc.name)
                {
                    return i;
                }
            }
            return -1;
        }

        internal void Remove(string name)
        {
            for (int i = 0; i < Descriptors.Count; i++)
            {
                if (((Descriptors[i].IsLocalAsset || Descriptors[i].IsGitPackage) && Descriptors[i].name == name) || Descriptors[i].name == name)
                {
                    AssetDescriptor desc = Descriptors[i];

                    Descriptors.RemoveAt(i);
                    DescriptorIDs.RemoveAt(i);
                    if (desc.isInstalled)
                    {
                        installedPackagesCache.Remove(desc);
                    }
                    else
                    {
                        notInstalledPackagesCache.Remove(desc);
                    }
                    availablePackagesCache.Add(desc);
                }
            }
            EditorUtility.SetDirty(this);
        }

        internal void InstallAllInPackage()
        {
            for (int i = 0; i < Descriptors.Count; i++)
            {
                if (Descriptors[i] == null)
                {
                    Debug.LogWarning("There was a missing AssetDescriptor in your AssetDescriptorList. It has been removed to prevent errors.");
                    Descriptors.RemoveAt(i);
                    EditorUtility.SetDirty(Descriptors[i]);
                }
                else if (!Descriptors[i].isInstalled)
                {
                    Descriptors[i].Install();
                    EditorUtility.SetDirty(Descriptors[i]);
                }
            }
        }

        internal void RefreshInstallStatusCache(List<AssetDescriptor> cachedPackagesAndAssets)
        {
            installedPackagesCache = new List<AssetDescriptor>();
            notInstalledPackagesCache = new List<AssetDescriptor>();
            availablePackagesCache = new List<AssetDescriptor>();
            int pos = -1;
            for (int i = 0; i < cachedPackagesAndAssets.Count; i++)
            {
                pos = GetPositionOf(cachedPackagesAndAssets[i]);
                if (pos >= 0 && pos < Descriptors.Count)
                {
                    if (Descriptors[pos].isInstalled)
                    {
                        installedPackagesCache.Add(Descriptors[pos]);
                    } else
                    {
                        notInstalledPackagesCache.Add(Descriptors[pos]);
                    }
                }
                else
                {
                    availablePackagesCache.Add(cachedPackagesAndAssets[i]);
                }
            }
        }

        internal virtual void OnGUI()
        {
        }
    }
}
