using TextAnimationsForUIToolkit.CustomAnimations;
using TextAnimationsForUIToolkit.Styles;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    [CustomPropertyDrawer(typeof(Template))]
    public class TemplatePropertyDrawer : PropertyDrawer
    {
        public VisualTreeAsset visualTree;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var customInspector = new VisualElement();

            visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                AssetDatabase.GUIDToAssetPath("007cbf041251d094d8560d4ae079461d")
            );

            if (visualTree == null)
            {
                Debug.LogError(
                    $"Visual Tree for {nameof(TemplatePropertyDrawer)} not set, using default UI."
                );
                return base.CreatePropertyGUI(property);
            }

            visualTree.CloneTree(customInspector);

            FoldoutName(property, customInspector);

            SetupTagValidation(property, customInspector);

            return customInspector;
        }

        private static void FoldoutName(SerializedProperty property, VisualElement customInspector)
        {
            var foldout = customInspector.Q<Foldout>("foldout");
            var tag = customInspector.Q<PropertyField>("tag");
            var tagProperty = property.FindPropertyRelative(tag.bindingPath);

            foldout.text = tagProperty.stringValue;
            tag.TrackPropertyValue(
                tagProperty,
                tagProperty =>
                {
                    foldout.text = tagProperty.stringValue;
                }
            );
        }

        private void SetupTagValidation(SerializedProperty property, VisualElement customInspector)
        {
            var error = customInspector.Q<VisualElement>("tag-name-error");
            var tag = customInspector.Q<PropertyField>("tag");
            var tagProperty = property.FindPropertyRelative(tag.bindingPath);

            ValidateTag(tagProperty, error);
            tag.TrackPropertyValue(
                tagProperty,
                tagProperty =>
                {
                    ValidateTag(tagProperty, error);
                }
            );
        }

        private void ValidateTag(SerializedProperty tagProperty, VisualElement error)
        {
            var isValid = CustomAnimationParser.IsValidName(tagProperty.stringValue);
            error.style.display = isValid ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
