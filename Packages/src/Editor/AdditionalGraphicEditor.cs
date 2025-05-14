using UnityEditor;
using UnityEngine;

namespace Swipeu.UIPrimitive.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(AdditionalGraphic), true)]
    public class AdditionalGraphicEditor : UnityEditor.Editor
    {
        SerializedProperty copyRoot;
        SerializedProperty copyInstance;

        SerializedProperty visualSettings;
        SerializedProperty color;
        SerializedProperty enableOutline;
        SerializedProperty enableGlow;
        SerializedProperty outlineWidth;
        SerializedProperty detailLevel;
        SerializedProperty antialiasing;
        SerializedProperty innerShadow;

        SerializedProperty transformSettings;
        SerializedProperty sizeOffset;
        SerializedProperty positionOffset;

        protected void OnEnable()
        {
            copyRoot = serializedObject.FindProperty("copyRoot");
            copyInstance = serializedObject.FindProperty("copyInstance");

            visualSettings = serializedObject.FindProperty("visualSettings");
            var property = visualSettings.Copy();
            property.NextVisible(true);
            color = property.Copy();
            property.NextVisible(false);
            enableOutline = property.Copy();
            property.NextVisible(false);
            enableGlow = property.Copy();
            property.NextVisible(false);
            outlineWidth = property.Copy();
            property.NextVisible(false);
            detailLevel = property.Copy();
            property.NextVisible(false);
            antialiasing = property.Copy();
            property.NextVisible(false);
            innerShadow = property.Copy();

            transformSettings = serializedObject.FindProperty("transformSettings");
            property = transformSettings.Copy();
            property.NextVisible(true);
            sizeOffset = property.Copy();
            property.NextVisible(false);
            positionOffset = property.Copy();
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(copyRoot);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(copyInstance);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(color);
            EditorGUILayout.PropertyField(antialiasing);
            EditorGUILayout.PropertyField(innerShadow);
            EditorGUILayout.PropertyField(enableOutline, GUILayout.ExpandWidth(false));
            if (enableOutline.boolValue && enableOutline.boolValue != enableGlow.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(outlineWidth, new GUIContent("width"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(enableGlow, GUILayout.ExpandWidth(false));
            if (enableGlow.boolValue && enableOutline.boolValue != enableGlow.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(outlineWidth, new GUIContent("width"));
                EditorGUILayout.PropertyField(detailLevel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(sizeOffset);
            EditorGUILayout.PropertyField(positionOffset);

            EditorGUILayout.Space();

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
