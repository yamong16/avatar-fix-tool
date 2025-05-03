using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using VRC.SDK3.Avatars.ScriptableObjects; // VRC ExpressionParameters

public class AvatarOptimizerWindow : EditorWindow
{
    private GameObject avatarRoot;
    private VRCExpressionParameters expressionParameters;
    private bool removePhysBones = true;
    private bool removeColliders = true;
    private bool removeEmptyObjects = true;
    private bool showVRAMEstimate = true;
    private bool analyzeExpressionParams = true;

    [MenuItem("Tools/Avatar Optimizer")]
    public static void ShowWindow()
    {
        GetWindow<AvatarOptimizerWindow>("Avatar Optimizer");
    }

    void OnGUI()
    {
        GUILayout.Label("Avatar Optimization Tool", EditorStyles.boldLabel);
        avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar Root", avatarRoot, typeof(GameObject), true);
        expressionParameters = (VRCExpressionParameters)EditorGUILayout.ObjectField("Expression Parameters", expressionParameters, typeof(VRCExpressionParameters), false);

        EditorGUILayout.Space();
        GUILayout.Label("Optimization Options", EditorStyles.label);

        removePhysBones = EditorGUILayout.Toggle("Remove PhysBones", removePhysBones);
        removeColliders = EditorGUILayout.Toggle("Remove Colliders", removeColliders);
        removeEmptyObjects = EditorGUILayout.Toggle("Remove Empty GameObjects", removeEmptyObjects);
        showVRAMEstimate = EditorGUILayout.Toggle("Show Texture VRAM Estimate", showVRAMEstimate);
        analyzeExpressionParams = EditorGUILayout.Toggle("Analyze Expression Parameters", analyzeExpressionParams);

        if (GUILayout.Button("Analyze and Optimize"))
        {
            OptimizeAvatar();
        }
    }

    void OptimizeAvatar()
    {
        if (avatarRoot == null)
        {
            Debug.LogWarning("Avatar Root is not assigned.");
            return;
        }

        int removedBones = 0, removedColliders = 0, removedEmpties = 0;
        float totalVRAM = 0f;

        if (removePhysBones)
        {
            var physBones = avatarRoot.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var pb in physBones)
            {
                if (pb.GetType().Name.Contains("PhysBone"))
                {
                    Undo.DestroyObjectImmediate(pb);
                    removedBones++;
                }
            }
        }

        if (removeColliders)
        {
            var colliders = avatarRoot.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                Undo.DestroyObjectImmediate(col);
                removedColliders++;
            }
        }

        if (removeEmptyObjects)
        {
            var transforms = avatarRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t != avatarRoot.transform && t.childCount == 0 && t.GetComponents<Component>().Length == 1)
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                    removedEmpties++;
                }
            }
        }

        if (showVRAMEstimate)
        {
            var renderers = avatarRoot.GetComponentsInChildren<Renderer>(true);
            HashSet<Texture> textures = new HashSet<Texture>();

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    foreach (var name in mat.GetTexturePropertyNames())
                    {
                        Texture tex = mat.GetTexture(name);
                        if (tex != null && textures.Add(tex))
                        {
                            int width = tex.width;
                            int height = tex.height;
                            int bpp = 4; // Assume 32-bit
                            totalVRAM += width * height * bpp / (1024f * 1024f);
                        }
                    }
                }
            }
        }

        if (analyzeExpressionParams && expressionParameters != null)
        {
            int totalCost = 0;
            foreach (var param in expressionParameters.parameters)
            {
                int cost = param.valueType switch
                {
                    VRCExpressionParameters.ValueType.Bool => 1,
                    VRCExpressionParameters.ValueType.Int => 4,
                    VRCExpressionParameters.ValueType.Float => 8,
                    _ => 0
                };
                totalCost += cost;
            }
            Debug.Log($"Expression Parameters Total Cost: {totalCost}/256");
        }

        Debug.Log($"Avatar Optimization Complete.\nRemoved PhysBones: {removedBones}\nRemoved Colliders: {removedColliders}\nRemoved Empty Objects: {removedEmpties}\nEstimated Texture VRAM Usage: {totalVRAM:F2} MB");
    }
}
