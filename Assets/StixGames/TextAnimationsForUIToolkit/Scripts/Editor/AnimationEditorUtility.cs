using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    public static class AnimationEditorUtility
    {
        public static void ConfigureAnimationEditor(
            VisualElement animation,
            AnimationConfig config, 
            SerializedObject serializedObject)
        {
            // Set toggle binding
            var toggle = animation.Q<Toggle>("animation__toggle");
            toggle.bindingPath = config.ToggleBindingPath;
            var toggleProperty = serializedObject.FindProperty(config.ToggleBindingPath);

            // Set content
            var contentProperty = serializedObject.FindProperty(config.ContentBindingPath);
            toggle.label = contentProperty.displayName;

            var content = animation.Q("animation__content");
            content.Clear();

            foreach (SerializedProperty childProperty in contentProperty)
            {
                content.Add(new PropertyField(childProperty));
            }

            content.style.display = toggleProperty.boolValue
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            toggle.TrackPropertyValue(
                toggleProperty,
                p =>
                {
                    var isOpen = p.boolValue;
                    content.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
                }
            );
        }
    }
}