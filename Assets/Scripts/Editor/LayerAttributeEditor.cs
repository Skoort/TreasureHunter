// CODE FROM: https://answers.unity.com/questions/609385/type-for-layer-selection.html

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LayerAttribute))]
class LayerAttributeEditor : PropertyDrawer
{

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // One line of oxygen free code.
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
    }
}
