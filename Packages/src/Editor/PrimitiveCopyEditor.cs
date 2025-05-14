using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Swipeu.UIPrimitive.Editor
{
    [CanEditMultipleObjects, CustomEditor(typeof(PrimitiveCopy), true)]
    public class PrimitiveCopyEditor : GraphicEditor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            base.RaycastControlsGUI();
            base.MaskableControlsGUI();

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
