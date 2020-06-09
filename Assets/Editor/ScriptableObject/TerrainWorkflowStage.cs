using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace threeDtbd.Workflow.PackageManagement
{
    /// <summary>
    /// A Terrain workflow stage represents project workflow worflow stage that 
    /// impacts the production of a Terrain within a scene. This includes heightmap,
    /// splatmap, terrain details and trees.
    /// 
    /// It contains a list of assets to support the creation of a Terrain.
    /// </summary>
    [CreateAssetMenu(fileName = "Terrain Workflow Stage", menuName = "3D TBD/Workflow/Terrain Stage")]
    public class TerrainWorkflowStage : WorkflowStage
    {
        // TODO: Handle the situation where the Terrains submodule is not in the usual place
        static string m_TerrainDirectory = "Assets/3dtbd/Terrains";
        static string m_TerrainSceneDirectory = m_TerrainDirectory + "/Scenes";
        static string m_TerrainDataDirectory = m_TerrainDirectory + "/Terrain Data";

        internal override void OnGUI()
        {
            base.OnGUI();
            if (GUILayout.Button("Export Terrain to " + m_TerrainSceneDirectory))
            {
                ExportTerrain();
            }
        }

        private void ExportTerrain()
        {
            // TODO make scene export parameters configurable
            string exportSceneName = "Heightmap_Only_" + DateTime.Now.ToFileTimeUtc();
            bool exportWithTextures = false;
            bool exportWithDetails = false;
            bool exportWithTrees = false;

            Directory.CreateDirectory(m_TerrainDataDirectory);

            Scene exportScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            exportScene.name = exportSceneName;

            CopyCameras(exportScene);
            CopyDirectionalLights(exportScene);
            CopyTerrains(m_TerrainDataDirectory, exportScene, exportWithTextures);

            EditorSceneManager.SaveScene(exportScene, m_TerrainSceneDirectory + "/" + exportSceneName + ".unity");
            EditorSceneManager.CloseScene(exportScene, true);
        }

        private static void CopyTerrains(string terrainDataDirectory, Scene exportScene, bool exportWithTextures = false, bool exportWithDetails = false, bool exportWithTrees = false )
        {
            Terrain[] terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++)
            {
                // Copy the Terrain Game Object
                Terrain newTerrain = CopyToScene(terrains[i].gameObject, exportScene).GetComponent<Terrain>();

                // Copy the Terrain Data
                TerrainData data = terrains[i].terrainData;
                string originalDataPath = AssetDatabase.GetAssetPath(data);
                string exportDataPath = terrainDataDirectory + "/" + data.name + ".asset";
                AssetDatabase.CopyAsset(originalDataPath, exportDataPath);
                TerrainData newData = AssetDatabase.LoadAssetAtPath<TerrainData>(exportDataPath);

                // Copy Textures?
                if (exportWithTextures)
                {
                    Debug.LogWarning("Not exporting textures just yet");
                }
                else
                {
                    newData.terrainLayers = null;
                }
                
                // Strip Details?
                if (!exportWithDetails)
                {
                    for (int l = 0; l < newData.detailPrototypes.Length; l++)
                    {
                        int[,] map = newData.GetDetailLayer(0, 0, newData.detailWidth, newData.detailHeight, l);
                        for (var y = 0; y < newData.detailHeight; y++)
                        {
                            for (var x = 0; x < newData.detailWidth; x++)
                            {
                                map[x, y] = 0;
                            }
                        }
                        newData.SetDetailLayer(0, 0, l, map);
                    }
                    
                    newData.detailPrototypes = new DetailPrototype[0];
                }

                // Strip Trees?
                if (!exportWithTrees)
                {
                    newData.treeInstances = new TreeInstance[0];
                    newData.treePrototypes = new TreePrototype[0];
                }

                newTerrain.terrainData = newData;
            }
        }

        private static void CopyDirectionalLights(Scene exportScene)
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    CopyToScene(lights[i].gameObject, exportScene);
                }
            }
        }

        private static void CopyCameras(Scene exportScene)
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                CopyToScene(cameras[i].transform.root.gameObject, exportScene);
            }
        }

    /// <summary>
    /// Copy a game object to a new scene and return the new object
    /// </summary>
    /// <param name="originalGo">The object to copy</param>
    /// <param name="toScene">The scene to copy to</param>
    /// <returns>The object in the new scene</returns>
    private static GameObject CopyToScene(GameObject originalGo, Scene toScene)
        {
            GameObject newGo = GameObject.Instantiate(originalGo); ;
            newGo.transform.parent = null;
            SceneManager.MoveGameObjectToScene(newGo, toScene);
            newGo.name = newGo.name.Substring(0, newGo.name.Length - "(Clone)".Length);
            return newGo;
        }
    }
}
