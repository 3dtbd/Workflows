using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace threeDtbd.Workflow.PackageManagement
{
    public class DeckardDescriptor : AssetDescriptor
    {
        public override string unityPackagePath
        {
            get { return @"OliVR\ScriptingVideo\Deckard Render.unitypackage"; }
        }

        internal override void OnGUI()
        {
            base.OnGUI();
            if (GUILayout.Button("Create Render Scene"))
            {
                string deckardScenePath = Application.dataPath + "/DeckardScenes";
                Directory.CreateDirectory(deckardScenePath);

                Scene originalScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                string fullPath = deckardScenePath + "/(Deckard) " + originalScene.name + ".unity";
                EditorSceneManager.SaveScene(originalScene, fullPath, true);

                EditorSceneManager.OpenScene(fullPath);
                Camera.main.enabled = false;

                //EditorSceneManager.OpenScene(originalScene.path);

                AssetDatabase.Refresh();
            }
        }
    }
}
