using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Swipeu.UIPrimitive.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(PrimitiveBase), true)]
    public class PrimitiveBaseEditor : GraphicEditor
    {
        SerializedProperty mainVisuals;
        SerializedProperty mainColor;
        SerializedProperty mainEnableOutline;
        SerializedProperty mainOutlineWidth;
        SerializedProperty mainAntialiasing;

        protected override void OnEnable()
        {
            mainVisuals = serializedObject.FindProperty("visualSettings");
            var property = mainVisuals.Copy();
            property.NextVisible(true);
            mainColor = property.Copy();
            property.NextVisible(false);
            mainEnableOutline = property.Copy();
            property.NextVisible(false);
            property.NextVisible(false);
            mainOutlineWidth = property.Copy();
            property.NextVisible(false);
            property.NextVisible(false);
            mainAntialiasing = property.Copy();

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            var property = serializedObject.GetIterator().Copy();

            bool addSpacer = false;
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == mainVisuals.name
                        || property.name == "transformSettings"
                        || property.name == "m_Script"
                        || property.name == "m_Color"
                        || property.name == "m_RaycastTarget"
                        || property.name == "m_RaycastPadding"
                        || property.name == "m_Maskable"
                        || property.name == "m_OnCullStateChanged"
                        || property.name == "m_Material"
                        )
                        continue;

                    EditorGUILayout.PropertyField(property, true);
                    addSpacer = true;
                }
                while (property.NextVisible(false));
            }

            if (addSpacer)
            {
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(mainColor);
            EditorGUILayout.PropertyField(mainAntialiasing);
            EditorGUILayout.PropertyField(mainEnableOutline);
            if (mainEnableOutline.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(mainOutlineWidth);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            base.RaycastControlsGUI();
            base.MaskableControlsGUI();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(PrefabUtility.IsPartOfPrefabInstance(target));
            if (GUILayout.Button("Add Shadow"))
            {
                PrimitiveBase primitiveBase = (PrimitiveBase)target;
                RectTransform root = (RectTransform)primitiveBase.transform.parent;
                primitiveBase.gameObject.AddComponent<AdditionalGraphic>().CopyRoot = root;
            }
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Add Overlay"))
            {
                PrimitiveBase primitiveBase = (PrimitiveBase)target;
                primitiveBase.gameObject.AddComponent<AdditionalGraphic>().CopyRoot = (RectTransform)primitiveBase.transform;
            }
            EditorGUILayout.EndHorizontal();

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
