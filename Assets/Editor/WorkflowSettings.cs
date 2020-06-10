using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace threeDtbd.Workflow
{
    public class WorkflowSettings
    {
        public static string ASSET_CACHE_DIR_PREF_KEY = "AssetCacheDirectory_" + Application.productName;
        public static string WORKFLOW_DATA_DIR_PREF_KEY = "WorkflowDataDirectory_" + Application.productName;
        public static string PROJECT_WORKFLOW_DATA = "ProjectWorkflowData_" + Application.productName;

        public static string assetCacheDirectory = "F:\\Unity\\Asset Store-5.x";
        public static string workflowDataDirectory;
        public static string descriptorsDataDirectory;
        public static string stagesDataDirectory;

        public static string[] openSourceGitURIs = new string[] { "git@github.com:3dtbd/Textures.git", 
            "git@github.com:3dtbd/DevLogger.git", "git@github.com:3dtbd/Models.git", 
            "git@github.com:3dtbd/VegetationStudioProExtensions.git",
            "git@github.com:3dtbd/nvjob-water-shader-simple-and-fast.git",
            "git@github.com:3dtbd/nvjob-sky-shader-simple-and-fast.git",
            "git@github.com:3dtbd/TheFirstPerson.git"};

        public static void Load()
        {
            SetupDataDirectories();
        }

        private static void SetupDataDirectories()
        {
            // TODO The default needs to be set to the normal Unity default directory 
            assetCacheDirectory = EditorPrefs.GetString(WorkflowSettings.ASSET_CACHE_DIR_PREF_KEY, assetCacheDirectory);
            workflowDataDirectory = EditorPrefs.GetString(WorkflowSettings.WORKFLOW_DATA_DIR_PREF_KEY, "Assets/Workflow");
            descriptorsDataDirectory = workflowDataDirectory + "/Asset Descriptors";
            stagesDataDirectory = workflowDataDirectory + "/Stages";

            Directory.CreateDirectory(assetCacheDirectory);
            Directory.CreateDirectory(workflowDataDirectory);
            Directory.CreateDirectory(descriptorsDataDirectory);
            Directory.CreateDirectory(stagesDataDirectory);
        }

        public static void Save()
        {
            EditorPrefs.SetString(WorkflowSettings.ASSET_CACHE_DIR_PREF_KEY, assetCacheDirectory);
            EditorPrefs.SetString(WorkflowSettings.WORKFLOW_DATA_DIR_PREF_KEY, workflowDataDirectory);
            AssetDatabase.SaveAssets();
        }
    }
}
