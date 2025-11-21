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

    SerializedProperty strengthScale;
    SerializedProperty falloffPower;
    SerializedProperty maxInfluenceRadius;
    SerializedProperty minDistance;

    private void OnEnable()
    {
        polarity = serializedObject.FindProperty("polarity");
        composite = serializedObject.FindProperty("composite");
        tilemap = serializedObject.FindProperty("tilemap");
        baseStrengthPerTile = serializedObject.FindProperty("baseStrengthPerTile");

        fieldMode = serializedObject.FindProperty("fieldMode");
        fixedDirection = serializedObject.FindProperty("fixedDirection");
        directionsInLocalSpace = serializedObject.FindProperty("directionsInLocalSpace");

        strengthScale = serializedObject.FindProperty("strengthScale");
        falloffPower = serializedObject.FindProperty("falloffPower");
        maxInfluenceRadius = serializedObject.FindProperty("maxInfluenceRadius");
        minDistance = serializedObject.FindProperty("minDistance");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Basic source settings
        EditorGUILayout.PropertyField(polarity);
        EditorGUILayout.PropertyField(composite);
        EditorGUILayout.PropertyField(tilemap);
        EditorGUILayout.PropertyField(baseStrengthPerTile);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Field Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fieldMode);

        // Show FixedDirectional-specific options only when selected
        if ((MagnetFieldMode)fieldMode.enumValueIndex == MagnetFieldMode.FixedDirectional)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fixedDirection, new GUIContent("Fixed Direction"));
            EditorGUILayout.PropertyField(directionsInLocalSpace, new GUIContent("Directions In Local Space"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Field Physics (per magnet)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(strengthScale);
        EditorGUILayout.PropertyField(falloffPower);
        EditorGUILayout.PropertyField(maxInfluenceRadius);
        EditorGUILayout.PropertyField(minDistance);

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
