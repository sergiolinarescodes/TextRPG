using TextAnimationsForUIToolkit.CustomAnimations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    [CustomEditor(typeof(CustomAnimationPreset))]
    public class CustomAnimationPresetEditor : Editor
    {
        public VisualTreeAsset visualTree;

        public StyleSheet lightModeStyle;
        public StyleSheet darkModeStyle;

        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();

            if (visualTree == null)
            {
                Debug.LogError(
                    $"Visual Tree for {nameof(CustomAnimationPreset)} not set, using default UI."
                );
                return base.CreateInspectorGUI();
            }

            visualTree.CloneTree(customInspector);

            if (EditorGUIUtility.isProSkin)
            {
                customInspector.styleSheets.Add(darkModeStyle);
            }
            else
            {
                customInspector.styleSheets.Add(lightModeStyle);
            }

            SetupTagField(customInspector);
            SetupAnimationSettings(customInspector);
            SetupAnimatedProperties(customInspector);

            return customInspector;
        }

        private void SetupTagField(VisualElement customInspector)
        {
            var tagNameError = customInspector.Q<VisualElement>("tag-name-error");

            var tagField = customInspector.Q<PropertyField>("tag");
            var tagProperty = serializedObject.FindProperty(tagField.bindingPath);

            ShowTagNameError(tagNameError, tagProperty);

            tagField.TrackPropertyValue(
                tagProperty,
                property => ShowTagNameError(tagNameError, property)
            );
        }

        private void ShowTagNameError(VisualElement tagNameError, SerializedProperty tagProperty)
        {
            var isValid = CustomAnimationParser.IsValidName(tagProperty.stringValue);
            tagNameError.style.display = isValid ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void SetupAnimationSettings(VisualElement customInspector)
        {
            var isTextAppearanceField = customInspector.Q<PropertyField>("is-text-appearance");
            var isTextVanishingField = customInspector.Q<PropertyField>("is-text-vanishing");

            var defaultFrequenzyField = customInspector.Q<PropertyField>("default-frequency");
            var defaultWaveSizeField = customInspector.Q<PropertyField>("default-wave-size");
            var defaultDurationField = customInspector.Q<PropertyField>("default-duration");

            var isTextAppearanceProperty = serializedObject.FindProperty(
                isTextAppearanceField.bindingPath
            );
            var isTextVanishingProperty = serializedObject.FindProperty(
                isTextVanishingField.bindingPath
            );

            SetFadeOrAnimationPropertiesEnabled(
                isTextAppearanceProperty,
                isTextVanishingProperty,
                defaultFrequenzyField,
                defaultWaveSizeField,
                defaultDurationField
            );
            isTextAppearanceField.TrackPropertyValue(
                isTextAppearanceProperty,
                property =>
                {
                    SetFadeOrAnimationPropertiesEnabled(
                        isTextAppearanceProperty,
                        isTextVanishingProperty,
                        defaultFrequenzyField,
                        defaultWaveSizeField,
                        defaultDurationField
                    );
                }
            );

            isTextVanishingField.TrackPropertyValue(
                isTextVanishingProperty,
                property =>
                {
                    SetFadeOrAnimationPropertiesEnabled(
                        isTextAppearanceProperty,
                        isTextVanishingProperty,
                        defaultFrequenzyField,
                        defaultWaveSizeField,
                        defaultDurationField
                    );
                }
            );
        }

        private static void SetFadeOrAnimationPropertiesEnabled(
            SerializedProperty isTextAppearanceProperty,
            SerializedProperty isTextVanishingProperty,
            PropertyField defaultFrequenzyField,
            PropertyField defaultWaveSizeField,
            PropertyField defaultDurationField
        )
        {
            var isAppearanceEffect = isTextAppearanceProperty.boolValue;
            var isVanishingEffect = isTextVanishingProperty.boolValue;
            var isFade = isAppearanceEffect || isVanishingEffect;
            defaultFrequenzyField.SetEnabled(!isFade);
            defaultWaveSizeField.SetEnabled(!isFade);
            defaultDurationField.SetEnabled(isFade);
        }

        private void SetupAnimatedProperties(VisualElement customInspector)
        {
            var colorField = customInspector.Q<PropertyField>("color");
            var animateColorField = customInspector.Q<Toggle>("animate-color");

            var animateColorProperty = serializedObject.FindProperty(
                animateColorField.bindingPath
            );

            colorField.SetEnabled(animateColorProperty.boolValue);
            animateColorField.TrackPropertyValue(
                animateColorProperty,
                property =>
                {
                    colorField.SetEnabled(property.boolValue);
                }
            );
        }
    }
}
