using System.Collections.Generic;
using System.Linq;
using TextAnimationsForUIToolkit.CustomAnimations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    [CustomEditor(typeof(TypewriterAnimationSettings))]
    public class TypewriterAnimationsEditor : Editor
    {
        public VisualTreeAsset visualTree;
        public StyleSheet lightModeStyle;
        public StyleSheet darkModeStyle;

        public VisualTreeAsset animationTree;
        private static readonly AnimationConfig[] fadeInAnimations =
        {
            new() { ToggleBindingPath = "useFadeIn", ContentBindingPath = "fadeIn", },
            new() { ToggleBindingPath = "useOffsetIn", ContentBindingPath = "offsetIn", },
            new()
            {
                ToggleBindingPath = "useRandomDirectionIn",
                ContentBindingPath = "randomDirectionIn",
            },
            new() { ToggleBindingPath = "useSizeIn", ContentBindingPath = "sizeIn", },
        };
        private static readonly AnimationConfig[] fadeOutAnimations =
        {
            new() { ToggleBindingPath = "useFadeOut", ContentBindingPath = "fadeOut", },
            new() { ToggleBindingPath = "useOffsetOut", ContentBindingPath = "offsetOut", },
            new()
            {
                ToggleBindingPath = "useRandomDirectionOut",
                ContentBindingPath = "randomDirectionOut",
            },
            new() { ToggleBindingPath = "useSizeOut", ContentBindingPath = "sizeOut", },
        };

        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();

            if (visualTree == null)
            {
                Debug.LogError(
                    $"Visual Tree for {nameof(TypewriterAnimationsEditor)} not set, using default UI."
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

            var fadeInAnimationContainer = customInspector.Q("fade-in-animations");
            var fadeOutAnimationContainer = customInspector.Q("fade-out-animations");

            foreach (var animation in fadeInAnimations)
            {
                AddAnimation(animation, fadeInAnimationContainer);
            }

            foreach (var animation in fadeOutAnimations)
            {
                AddAnimation(animation, fadeOutAnimationContainer);
            }

            CheckCustomAnimations(customInspector);

            CustomAnimationEditorUtility.SetupCreateCustomAnimationButton(
                customInspector,
                serializedObject,
                true
            );

            return customInspector;
        }

        private void AddAnimation(AnimationConfig config, VisualElement parent)
        {
            var animation = new VisualElement();
            animationTree.CloneTree(animation);

            AnimationEditorUtility.ConfigureAnimationEditor(animation, config, serializedObject);
            parent.Add(animation);
        }

        private void CheckCustomAnimations(VisualElement customInspector)
        {
            var noTypewriterAnimationErrorBox = customInspector.Q(
                "only-typewriter-animations-allowed"
            );
            var invalidAnimationsList = customInspector.Q<Label>(
                "custom-animations-without-typewriter"
            );

            var propertyField = customInspector.Q<PropertyField>("custom-animations");
            var property = serializedObject.FindProperty(propertyField.bindingPath);

            CheckAndShowError(property, noTypewriterAnimationErrorBox, invalidAnimationsList);

            propertyField.TrackPropertyValue(
                property,
                property =>
                {
                    CheckAndShowError(
                        property,
                        noTypewriterAnimationErrorBox,
                        invalidAnimationsList
                    );
                }
            );
        }

        private void CheckAndShowError(
            SerializedProperty property,
            VisualElement noTypewriterAnimationErrorBox,
            Label invalidAnimationsList
        )
        {
            var errorElements = new List<string>();
            for (var i = 0; i < property.arraySize; i++)
            {
                var arrayElement = property.GetArrayElementAtIndex(i);
                if (arrayElement.objectReferenceValue == null)
                {
                    continue;
                }

                var animation = (CustomAnimationPreset)arrayElement.objectReferenceValue;
                if (!animation.isTextAppearanceEffect && !animation.isTextVanishingEffect)
                {
                    errorElements.Add(animation.name);
                }
            }

            noTypewriterAnimationErrorBox.style.display =
                errorElements.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            invalidAnimationsList.text = string.Join("\n", errorElements.Select(x => $"• {x}"));
        }
    }
}