using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using Deformer;


namespace Deformer
{
    public static class MeshUtils
    {

        public static Mesh GetMesh(GameObject obj)
        {
            Mesh curMesh = null;
            if (obj == null) return curMesh;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();

            if (smr)
                curMesh = smr.sharedMesh;
            else if (mf)
                curMesh = mf.sharedMesh;

            return curMesh;
        }

        public static MeshDescription CreateMeshDescription(GameObject obj)
        {
            Mesh mesh = GetMesh(obj);
            if (mesh == null) return null;

            // create folder paths
            string folderPath = Directory.CreateDirectory("Assets/Resources").Name;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string folderPathB = Directory.CreateDirectory("Assets/Resources/MeshData").Name;
            if (!Directory.Exists(folderPathB))
            {
                Directory.CreateDirectory(folderPathB);
            }

            string meshName = mesh.name;
            string meshNiceName = Utils.GetNiceName(meshName);

            string name = "MeshData/" +meshNiceName + "_MeshDescription";
            MeshDescription asset = (MeshDescription)Resources.Load<MeshDescription>(name);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MeshDescription>();
                string p = folderPath + "/" + name;
                AssetDatabase.CreateAsset(asset, "Assets/" + p + ".asset");


                asset.Name = meshName;
                asset.NiceName = meshNiceName;
                asset.VertCount = MeshUtils.GetMesh(obj).vertexCount;
                asset.Mesh = MeshUtils.GetMesh(obj);
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(asset);
            }

            MeshRenderer render = obj.GetComponent<MeshRenderer>();
            return asset;
        }
    }
}
