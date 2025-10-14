using Swipeu.UIPrimitive;
using UnityEditor;
using UnityEngine;

namespace Swipeu.UIPrimitive.Editor
{
    [CustomPropertyDrawer(typeof(PrimitivePolygon.PolygonPoint))]
    public class PolygonPointDrawer : PropertyDrawer
    {
        enum AlignmnentCategory
        {
            POINT,
            ROUNDED,
            ROUNDED_STRETCHED
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var alignmentProp = property.FindPropertyRelative("Alignment");
            var alignment = (PrimitivePolygon.Alignment)alignmentProp.enumValueIndex;
            var alignmentType = GetAlignmentType(alignment);

            // Always show foldout
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                // Alignment
                height += EditorGUIUtility.singleLineHeight + 2;

                switch (alignmentType)
                {
                    case AlignmnentCategory.ROUNDED:
                    case AlignmnentCategory.ROUNDED_STRETCHED:
                        height += (EditorGUIUtility.singleLineHeight + 2) * 2;
                        break;

                    default:
                        height += EditorGUIUtility.singleLineHeight + 2;
                        break;
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var alignmentProp = property.FindPropertyRelative("Alignment");
            var pointProp = property.FindPropertyRelative("Point");
            var smoothnessProp = property.FindPropertyRelative("Smoothness");
            var radiusProp = property.FindPropertyRelative("Radius");
            var radiusVecProp = property.FindPropertyRelative("Radius_vec");

            // Draw foldout
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.y += EditorGUIUtility.singleLineHeight + 2;

                // Alignment
                EditorGUI.PropertyField(position, alignmentProp);

                var alignment = (PrimitivePolygon.Alignment)alignmentProp.enumValueIndex;
                var alignmentType = GetAlignmentType(alignment);

                switch (alignmentType)
                {
                    case AlignmnentCategory.ROUNDED:
                        position.y += EditorGUIUtility.singleLineHeight + 2;
                        EditorGUI.PropertyField(position, smoothnessProp);
                        position.y += EditorGUIUtility.singleLineHeight + 2;
                        EditorGUI.PropertyField(position, radiusProp);
                        break;


                    case AlignmnentCategory.ROUNDED_STRETCHED:
                        position.y += EditorGUIUtility.singleLineHeight + 2;
                        EditorGUI.PropertyField(position, smoothnessProp);
                        position.y += EditorGUIUtility.singleLineHeight + 2;
                        EditorGUI.PropertyField(position, radiusVecProp);
                        break;

                    default:
                        position.y += EditorGUIUtility.singleLineHeight + 2;
                        EditorGUI.PropertyField(position, pointProp);
                        break;
                }


                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private AlignmnentCategory GetAlignmentType(PrimitivePolygon.Alignment alignment)
        {
            switch (alignment)
            {
                case PrimitivePolygon.Alignment.Absolute:
                case PrimitivePolygon.Alignment.BottomLeft:
                case PrimitivePolygon.Alignment.TopLeft:
                case PrimitivePolygon.Alignment.TopRight:
                case PrimitivePolygon.Alignment.BottomRight:
                    return AlignmnentCategory.POINT;


                case PrimitivePolygon.Alignment.RoundedBottomLeft:
                case PrimitivePolygon.Alignment.RoundedTopLeft:
                case PrimitivePolygon.Alignment.RoundedTopRight:
                case PrimitivePolygon.Alignment.RoundedBottomRight:
                    return AlignmnentCategory.ROUNDED;

                case PrimitivePolygon.Alignment.RoundedStretchedBottomLeft:
                case PrimitivePolygon.Alignment.RoundedStretchedTopLeft:
                case PrimitivePolygon.Alignment.RoundedStretchedTopRight:
                case PrimitivePolygon.Alignment.RoundedStretchedBottomRight:
                    return AlignmnentCategory.ROUNDED_STRETCHED;

                default:
                    return AlignmnentCategory.POINT;
            }
        }
    }
}