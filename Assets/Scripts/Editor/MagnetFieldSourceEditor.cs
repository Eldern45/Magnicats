using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MagnetFieldSource))]
public class MagnetFieldSourceEditor : Editor
{
    SerializedProperty polarity;
    SerializedProperty composite;
    SerializedProperty tilemap;
    SerializedProperty baseStrengthPerTile;

    SerializedProperty fieldMode;
    SerializedProperty fixedDirection;
    SerializedProperty directionsInLocalSpace;

    // FixedDirectional: raycast settings
    SerializedProperty raycastLayerMask;

    SerializedProperty strengthScale;
    SerializedProperty falloffPower;
    SerializedProperty maxInfluenceRadius;
    SerializedProperty minDistance;
    SerializedProperty vfxPrefab;
    SerializedProperty vfxRadiusMultiplier;
    SerializedProperty vfxWidthMultiplier;
    SerializedProperty vfxSpeed;
    SerializedProperty vfxOpacity;

    private void OnEnable()
    {
        polarity = serializedObject.FindProperty("polarity");
        composite = serializedObject.FindProperty("composite");
        tilemap = serializedObject.FindProperty("tilemap");
        baseStrengthPerTile = serializedObject.FindProperty("baseStrengthPerTile");

        fieldMode = serializedObject.FindProperty("fieldMode");
        fixedDirection = serializedObject.FindProperty("fixedDirection");
        directionsInLocalSpace = serializedObject.FindProperty("directionsInLocalSpace");

        raycastLayerMask = serializedObject.FindProperty("raycastLayerMask");

        strengthScale = serializedObject.FindProperty("strengthScale");
        falloffPower = serializedObject.FindProperty("falloffPower");
        maxInfluenceRadius = serializedObject.FindProperty("maxInfluenceRadius");
        minDistance = serializedObject.FindProperty("minDistance");
        vfxPrefab = serializedObject.FindProperty("vfxPrefab");
        vfxRadiusMultiplier = serializedObject.FindProperty("vfxRadiusMultiplier");
        vfxWidthMultiplier = serializedObject.FindProperty("vfxWidthMultiplier");
        vfxSpeed = serializedObject.FindProperty("vfxSpeed");
        vfxOpacity = serializedObject.FindProperty("vfxOpacity");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Basic source settings
        EditorGUILayout.PropertyField(polarity);
        EditorGUILayout.PropertyField(composite);
        EditorGUILayout.PropertyField(tilemap);
        EditorGUILayout.PropertyField(baseStrengthPerTile);

        EditorGUILayout.PropertyField(fieldMode);

        // Show FixedDirectional-specific options only when selected
        if ((MagnetFieldMode)fieldMode.enumValueIndex == MagnetFieldMode.FixedDirectional)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fixedDirection, new GUIContent("Fixed Direction"));
            EditorGUILayout.PropertyField(directionsInLocalSpace, new GUIContent("Directions In Local Space"));
            EditorGUILayout.PropertyField(raycastLayerMask, new GUIContent("Raycast Layer Mask"));
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.PropertyField(strengthScale);
        EditorGUILayout.PropertyField(falloffPower);
        EditorGUILayout.PropertyField(maxInfluenceRadius);
        EditorGUILayout.PropertyField(minDistance);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Effects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(vfxPrefab);
        EditorGUILayout.PropertyField(vfxRadiusMultiplier);
        EditorGUILayout.PropertyField(vfxWidthMultiplier);
        EditorGUILayout.PropertyField(vfxSpeed);
        EditorGUILayout.PropertyField(vfxOpacity);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Rebuild Regions"))
            {
                foreach (var t in targets)
                {
                    var src = (MagnetFieldSource)t;
                    Undo.RecordObject(src, "Rebuild Magnet Regions");
                    src.Rebuild();
                    EditorUtility.SetDirty(src);
                }
            }
        }
    }
}
