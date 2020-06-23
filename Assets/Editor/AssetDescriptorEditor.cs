using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace threeDtbd.Workflow.PackageManagement
{
    [CustomEditor(typeof(AssetDescriptor), true)]
    public class AssetDescriptorEditor : Editor
    {
        AssetDescriptor desc;

        public void OnEnable()
        {
            desc = (AssetDescriptor)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PrefixLabel("Documentation");
            desc.documentation = EditorGUILayout.TextArea(desc.documentation, GUILayout.Height(240));
            desc.unityForum = EditorGUILayout.TextField("Forum", desc.unityForum);
            desc.gitURI = EditorGUILayout.TextField("Git URI", desc.gitURI);
            desc.unityPackagePath = EditorGUILayout.TextField("Package Path", desc.unityPackagePath);
            desc.scriptDefines = EditorGUILayout.TextField("Script Defines", desc.scriptDefines);
            desc.isInstalled = EditorGUILayout.ToggleLeft("Is Installed", desc.isInstalled);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(desc.name, EditorStyles.largeLabel);
            desc.OnGUI();
        }
    }
}
