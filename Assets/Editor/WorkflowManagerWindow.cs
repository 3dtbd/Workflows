using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using WizardsCode.Controller;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace threeDtbd.Workflow.PackageManagement
{
    public class WorkflowManagerWindow : EditorWindow
    {
        public ProjectWorkflow workflow;
        
        private double refreshFrequency = 2;
        private double nextRefreshTime = 0;

        private enum Tab { Workflow, Settings }
        private int selectedTab;
        private static List<AssetDescriptor> _cachedPackagesAndAssets;
        private static SearchRequest searchRequest;
        private Vector2 installedPackageListScrollPos;
        private Vector2 notInstalledPackageListScrollPos;
        private Vector2 availablePackageListScrollPos;
        private string filterText;

        List<AssetDescriptor> updatedPackageList;
        private WorkflowStage newWorkflowStage;
        private string newWorkflowStageName;

        private List<AssetDescriptor> CachedPackagesAndAssets
        {
            get {
                if (EditorApplication.timeSinceStartup > nextRefreshTime && (searchRequest == null || searchRequest.IsCompleted))
                {
                    RefreshPackageList();
                    nextRefreshTime = EditorApplication.timeSinceStartup + refreshFrequency;
                }
                return _cachedPackagesAndAssets;  
            }
            set
            {
                _cachedPackagesAndAssets = value;
                for (int i = 0; i < workflow.stages.Count; i++)
                {
                    if (workflow.stages[i] != null)
                    {
                        workflow.stages[i].RefreshInstallStatusCache(_cachedPackagesAndAssets);
                    }
                }
            }
        }

        [MenuItem("Tools/3D TBD/Workflow Manager")]
        public static void ShowWindow()
        {
            GetWindow<WorkflowManagerWindow>(false, "Workflow Manager", true);
        }

        private void OnEnable()
        {
            WorkflowSettings.Load();
            string path = EditorPrefs.GetString(WorkflowSettings.PROJECT_WORKFLOW_DATA);
            workflow = AssetDatabase.LoadAssetAtPath(path, typeof(ProjectWorkflow)) as ProjectWorkflow;
        }

        private void OnDisable()
        {
            WorkflowSettings.Save();
            EditorPrefs.SetString(WorkflowSettings.PROJECT_WORKFLOW_DATA, AssetDatabase.GetAssetPath(workflow));
        }

        void OnGUI()
        {   
            selectedTab = GUILayout.Toolbar(selectedTab, Enum.GetNames(typeof(Tab)));
            switch (selectedTab)
            {
                case (int)Tab.Workflow:
                    if (workflow == null)
                    {
                        OnSetupConfigurationGUI();
                    }
                    else
                    {
                        OnWorkflowGUI();
                    }
                    break;
                case (int)Tab.Settings:
                    OnSettingsGUI();
                    break;
            }
        }

        private void OnSetupConfigurationGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Workflow configuration");
            workflow = EditorGUILayout.ObjectField(workflow, typeof(ProjectWorkflow), false) as ProjectWorkflow;
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create New Workflow Configuration"))
            {
                workflow = ScriptableObject.CreateInstance<ProjectWorkflow>();
                string path = AssetDatabase.GenerateUniqueAssetPath(WorkflowSettings.workflowDataDirectory + "/" + Application.productName + " Workflow.asset");
                AssetDatabase.CreateAsset(workflow, path);
                AssetDatabase.SaveAssets();
            }

            if (workflow != null)
            {
                EditorUtility.SetDirty(workflow);
            }
        }

        private void OnNewStageGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Stage Name");
            newWorkflowStageName = EditorGUILayout.TextField(newWorkflowStageName);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newWorkflowStageName));
            if (GUILayout.Button("Create New Workflow Stage"))
            {
                string path = WorkflowSettings.stagesDataDirectory + "/" +  newWorkflowStageName + ".asset";
                newWorkflowStage = AssetDatabase.LoadAssetAtPath<WorkflowStage>(path);
                if (newWorkflowStage == null)
                {
                    newWorkflowStage = ScriptableObject.CreateInstance<WorkflowStage>();
                    AssetDatabase.CreateAsset(newWorkflowStage, path);
                    AssetDatabase.SaveAssets();
                } else
                {
                    if (workflow.ContainsStage(newWorkflowStage))
                    {
                        EditorUtility.DisplayDialog("Workflow Stage Already Exists", "There is already a Workflow Stage with the name '" + newWorkflowStageName + "' in your project.", "OK");
                    }
                    else
                    {
                        if (!EditorUtility.DisplayDialog("Use existing Workflow Stage?", "There is already a Workflow Stage with the name '" + newWorkflowStageName + "'.\nDo you want to add it to your Workflow?", "Yes", "No"))
                        {
                            newWorkflowStage = null;
                        }
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("New Stage");
            newWorkflowStage = EditorGUILayout.ObjectField(newWorkflowStage, typeof(WorkflowStage), false) as WorkflowStage;
            if (newWorkflowStage != null)
            {
                if (!workflow.stages.Contains(newWorkflowStage))
                {
                    workflow.stages.Add(newWorkflowStage);
                    workflow.expandPackageListInGUI.Add(false);
                    newWorkflowStage = null;

                    EditorUtility.SetDirty(workflow);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnWorkflowGUI()
        {
            OnNewStageGUI();

            for (int i = 0; i < workflow.stages.Count; i++) {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                if (workflow.stages[i] != null)
                {
                    workflow.expandPackageListInGUI[i] = EditorGUILayout.Foldout(workflow.expandPackageListInGUI[i], workflow.stages[i].name + " ( " + workflow.stages[i].UninstalledCount + " of " + workflow.stages[i].Count + " installed)");
                } else
                {
                    workflow.expandPackageListInGUI[i] = EditorGUILayout.Foldout(workflow.expandPackageListInGUI[i], "Undefined List");
                }
                if (workflow.expandPackageListInGUI[i])
                {
                    OnWorkflowStageGUI(i);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void OnWorkflowStageGUI(int index)
        {
            WorkflowStage stage = workflow.stages[index];

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Package Set");
            workflow.stages[index] = EditorGUILayout.ObjectField(workflow.stages[index], typeof(WorkflowStage), false) as WorkflowStage;

            if (GUILayout.Button("Remove from project"))
            {
                workflow.stages.RemoveAt(index);
                workflow.expandPackageListInGUI.RemoveAt(index);
                newWorkflowStage = null;
            }

            if (index > workflow.stages.Count || workflow.stages[index] == null)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }
            EditorGUILayout.EndHorizontal();

            workflow.stages[index].OnGUI();

            int count = workflow.stages[index].NotInstalledCount;
            if (count > 0)
            {
                if (GUILayout.Button("Install " + count + " missing", GUILayout.Height(180)))
                {
                    workflow.stages[index].InstallAllInPackage();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter");
            filterText = EditorGUILayout.TextField(filterText);
            EditorGUILayout.EndHorizontal();

            if (CachedPackagesAndAssets != null)
            {
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Installed", EditorStyles.boldLabel);
                installedPackageListScrollPos = EditorGUILayout.BeginScrollView(installedPackageListScrollPos);
                for (int i = 0; i < stage.installedPackagesCache.Count; i++)
                {
                    if (stage.installedPackagesCache[i].name.ToLower().Contains(filterText.ToLower()))
                    {
                        OnInstalledPackageGUI(stage, stage.installedPackagesCache[i]);
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Not Installed", EditorStyles.boldLabel);
                notInstalledPackageListScrollPos = EditorGUILayout.BeginScrollView(notInstalledPackageListScrollPos);
                for (int i = 0; i < stage.notInstalledPackagesCache.Count; i++)
                {
                    if (stage.notInstalledPackagesCache[i].name.ToLower().Contains(filterText.ToLower()))
                    {
                        OnNotInstalledPackageGUI(stage, stage.notInstalledPackagesCache[i]);
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Available", EditorStyles.boldLabel);
                availablePackageListScrollPos = EditorGUILayout.BeginScrollView(availablePackageListScrollPos, GUILayout.MaxHeight(200));
                for (int i = 0; i < stage.availablePackagesCache.Count; i++)
                {
                    if (stage.availablePackagesCache[i].name.ToLower().Contains(filterText.ToLower()))
                    {
                        OnAvailablePackageGUI(stage, stage.availablePackagesCache[i]);
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }

        private void OnInstalledPackageGUI(WorkflowStage stage, AssetDescriptor desc)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(desc.name);
            if (GUILayout.Button("Remove from Workflow Stage"))
            {
                stage.Remove(desc.name);
            }
            EditorGUILayout.EndHorizontal();
            desc.OnGUI();
        }

        private void OnNotInstalledPackageGUI(WorkflowStage stage, AssetDescriptor desc)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(desc.name);
            if (GUILayout.Button("Install"))
            {
                desc.Install();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnAvailablePackageGUI(WorkflowStage stage, AssetDescriptor desc)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(desc.name);
            if (GUILayout.Button("Add to Workflow Stage"))
            {
                stage.Add(desc);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshPackageList()
        {
            updatedPackageList = new List<AssetDescriptor>();

            // Local Assets
            // TODO make the location of local asset cache configurable
            ProcessAssetsDirectory(WorkflowSettings.assetCacheDirectory);

            // 3D TBD Collection
            for (int idx = 0; idx < WorkflowSettings.openSourceGitURIs.Length; idx++)
            {
                updatedPackageList.Add(AssetDescriptor.CreateInstanceFromGitHub(WorkflowSettings.openSourceGitURIs[idx]));
            }

            // Internal Packages
            searchRequest = Client.SearchAll();
            EditorApplication.update += PackageManagerListRequestProgress;
        }

        void ProcessAssetsDirectory(string targetDirectory)
        {
            List<string> packages = Directory.Exists(targetDirectory)
                                  ? Directory
                                      .EnumerateFiles(targetDirectory, "*.unitypackage", SearchOption.AllDirectories)
                                      .ToList()
                                  : new List<string>();
            foreach (string package in packages)
            {
                AssetDescriptor desc = ScriptableObject.CreateInstance<AssetDescriptor>();
                desc.unityPackagePath = package;
                desc.name = Path.GetFileNameWithoutExtension(package);
                desc.packageType = AssetDescriptor.PackageType.LocalAsset;

                updatedPackageList.Add(desc);
            }
        }

        void PackageManagerListRequestProgress()
        {
            if (searchRequest.IsCompleted)
            {
                if (searchRequest.Status == StatusCode.Success)
                {
                    foreach (PackageInfo package in searchRequest.Result)
                    {
                        AssetDescriptor desc = ScriptableObject.CreateInstance<AssetDescriptor>();
                        desc.name = package.displayName; 
                        desc.id = package.packageId;
                        desc.unityPackagePath = package.assetPath;
                        desc.packageType = AssetDescriptor.PackageType.Package;
                        updatedPackageList.Add(desc);
                    }
                }
                else if (searchRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError(searchRequest.Error.message);
                }

                EditorApplication.update -= PackageManagerListRequestProgress;

                CachedPackagesAndAssets = updatedPackageList.OrderBy(x => x.name).ToList();
            }
        }

        private void OnSettingsGUI()
        {
            OnSetupConfigurationGUI();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Asset Cache Directory");
            WorkflowSettings.assetCacheDirectory = EditorGUILayout.TextField(WorkflowSettings.assetCacheDirectory);
            if (GUILayout.Button("Browse"))
            {
                WorkflowSettings.assetCacheDirectory = EditorUtility.OpenFolderPanel("Select Asset Cache Folder", WorkflowSettings.assetCacheDirectory, "");
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Data Directory");
            WorkflowSettings.workflowDataDirectory = EditorGUILayout.TextField(WorkflowSettings.workflowDataDirectory);
            if (GUILayout.Button("Browse"))
            {
                WorkflowSettings.workflowDataDirectory = EditorUtility.OpenFolderPanel("Select Asset Cache Folder", WorkflowSettings.workflowDataDirectory, "");
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}