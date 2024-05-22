using UnityEditor;
using UnityEngine;

namespace CosmicShore.Utility.UI
{
    /// <summary>
    /// A drawer to expose each of the values in a <see cref="WeightedSpawnable" />.
    /// </summary>
    [CustomPropertyDrawer(typeof(WeightedSpawnable))]
    public class WeightedSpawnableUIE : PropertyDrawer
    {
        /// <summary>
        /// The distance by which the middle of the fields will be shifted left in the editor.
        /// </summary>
        private const int SHIFT_LEFT = 40;
        /// <summary>
        /// The distance by which the weight the fields will be shifted right in the editor.
        /// </summary>
        private const int SHIFT_RIGHT = 5;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Keyboard), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var firstRect = new Rect(position.x, position.y, position.width / 2 + SHIFT_LEFT, position.height);
            var secondRect = new Rect(position.x + position.width / 2 + SHIFT_LEFT + SHIFT_RIGHT, position.y, position.width / 2 - SHIFT_LEFT - SHIFT_RIGHT, position.height);

            var labelWidth = EditorGUIUtility.labelWidth;

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUIUtility.labelWidth = 30;
            EditorGUI.PropertyField(firstRect, property.FindPropertyRelative("Spawnable"), new GUIContent("Seg."), true);

            EditorGUIUtility.labelWidth = 10;
            EditorGUI.PropertyField(secondRect, property.FindPropertyRelative("Weight"), new GUIContent("⚖"), true);
        
            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.EndProperty();
        }
    }
}
