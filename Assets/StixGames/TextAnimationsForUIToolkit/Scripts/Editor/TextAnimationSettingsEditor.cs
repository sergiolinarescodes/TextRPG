using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    [CustomEditor(typeof(TextAnimationSettings))]
    public class TextAnimationSettingsEditor : Editor
    {
        public VisualTreeAsset visualTree;
        public VisualTreeAsset animationTree;

        public StyleSheet lightModeStyle;
        public StyleSheet darkModeStyle;

        // Define animation configurations
        private static readonly AnimationConfig[] defaultAnimations =
        {
            new() { ToggleBindingPath = "defaultAnimations.useBounce", ContentBindingPath = "defaultAnimations.bounce" },
            new() { ToggleBindingPath = "defaultAnimations.useRainbow", ContentBindingPath = "defaultAnimations.rainbow" },
            new() { ToggleBindingPath = "defaultAnimations.useRotate", ContentBindingPath = "defaultAnimations.rotate" },
            new() { ToggleBindingPath = "defaultAnimations.useShake", ContentBindingPath = "defaultAnimations.shake" },
            new() { ToggleBindingPath = "defaultAnimations.useSizeWave", ContentBindingPath = "defaultAnimations.sizeWave" },
            new() { ToggleBindingPath = "defaultAnimations.useSwing", ContentBindingPath = "defaultAnimations.swing" },
            new() { ToggleBindingPath = "defaultAnimations.useWave", ContentBindingPath = "defaultAnimations.wave" },
            new() { ToggleBindingPath = "defaultAnimations.useWiggle", ContentBindingPath = "defaultAnimations.wiggle" },
        };

        public override VisualElement CreateInspectorGUI()
        {
            var customInspector = new VisualElement();

            if (visualTree == null)
            {
                Debug.LogError(
                    $"Visual Tree for {nameof(TextAnimationSettings)} not set, using default UI."
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

            SetupTypewriterSettings(customInspector);

            SetupAnimationSettings(customInspector);

            SetupDebugSettings(customInspector);

            return customInspector;
        }

        private void SetupTypewriterSettings(VisualElement customInspector)
        {
            var appearanceToggle = customInspector.Q<Toggle>("appearance__toggle");
            var appearanceToggleProperty = serializedObject.FindProperty(
                appearanceToggle.bindingPath
            );
            var appearanceSettings = customInspector.Q("appearance__settings");
            var vanishingToggle = customInspector.Q<Toggle>("vanishing__toggle");
            var vanishingToggleProperty = serializedObject.FindProperty(
                vanishingToggle.bindingPath
            );
            var vanishingSettings = customInspector.Q("vanishing__settings");
            var generalSettings = customInspector.Q("typewriter-general-settings");

            appearanceSettings.SetEnabled(appearanceToggleProperty.boolValue);
            appearanceToggle.TrackPropertyValue(
                appearanceToggleProperty,
                property =>
                {
                    appearanceSettings.SetEnabled(property.boolValue);

                    var enableGeneral =
                        appearanceToggleProperty.boolValue | vanishingToggleProperty.boolValue;
                    generalSettings.SetEnabled(enableGeneral);
                }
            );

            vanishingSettings.SetEnabled(vanishingToggleProperty.boolValue);
            vanishingToggle.TrackPropertyValue(
                vanishingToggleProperty,
                property =>
                {
                    vanishingSettings.SetEnabled(property.boolValue);

                    var enableGeneral =
                        appearanceToggleProperty.boolValue | vanishingToggleProperty.boolValue;
                    generalSettings.SetEnabled(enableGeneral);
                }
            );
        }

        private void SetupAnimationSettings(VisualElement customInspector)
        {
            var createDefaultAnimationButton = customInspector.Q<Button>(
                "create-default-animation-button"
            );
            var createFallbackAnimationButton = customInspector.Q<Button>(
                "create-fallback-animation-button"
            );

            var defaultAnimations = customInspector.Q<PropertyField>("default-typewriter-animations");
            var fallbackAnimations = customInspector.Q<PropertyField>("fallback-typewriter-animations");

            var defaultAnimationsProperty = serializedObject.FindProperty(
                defaultAnimations.bindingPath
            );
            var fallbackAnimationsProperty = serializedObject.FindProperty(
                fallbackAnimations.bindingPath
            );

            SetupCreateTypewriterButton(
                createDefaultAnimationButton,
                defaultAnimationsProperty,
                defaultAnimations
            );
            SetupCreateTypewriterButton(
                createFallbackAnimationButton,
                fallbackAnimationsProperty,
                fallbackAnimations
            );
            CustomAnimationEditorUtility.SetupCreateCustomAnimationButton(
                customInspector,
                serializedObject,
                false
            );
            
            // Configure animation editors for each animation
            var animationsContainer = customInspector.Q("default-animations-container");
            if (animationsContainer != null && animationTree != null)
            {
                foreach (var defaultAnimation in TextAnimationSettingsEditor.defaultAnimations)
                {
                    var animation = new VisualElement();
                    animationTree.CloneTree(animation);
                    AnimationEditorUtility.ConfigureAnimationEditor(
                        animation,
                        defaultAnimation,
                        serializedObject
                    );
                    animationsContainer.Add(animation);
                }
            }
        }

        private void SetupCreateTypewriterButton(
            Button button,
            SerializedProperty property,
            PropertyField field
        )
        {
            SetDisplayed(button, property.objectReferenceValue == null);
            field.TrackPropertyValue(
                property,
                property =>
                {
                    SetDisplayed(button, property.objectReferenceValue == null);
                }
            );

            button.RegisterCallback<ClickEvent, SerializedProperty>(
                OnCreateTypewriterSettingsButtonClicked,
                property
            );
        }

        private void OnCreateTypewriterSettingsButtonClicked(
            ClickEvent evt,
            SerializedProperty property
        )
        {
            var path = EditorUtility.SaveFilePanel(
                "Create typewriter animation settings",
                "Assets",
                "Typewriter Animation Settings",
                "asset"
            );
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);

            var instance = CreateInstance<TypewriterAnimationSettings>();
            AssetDatabase.CreateAsset(instance, path);

            property.objectReferenceValue = instance;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetDisplayed(Button button, bool setVisible)
        {
            button.style.display = setVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetupDebugSettings(VisualElement customInspector)
        {
            var enableAnimationDebuggingPropertyField = customInspector.Q<PropertyField>(
                "enable-animation-debugging"
            );

            var enableAnimationDebuggingProperty = serializedObject.FindProperty(
                enableAnimationDebuggingPropertyField.bindingPath
            );

            var debugAnimationTimeElement = customInspector.Q("debug-animation-time");

            debugAnimationTimeElement.SetEnabled(enableAnimationDebuggingProperty.boolValue);
            enableAnimationDebuggingPropertyField.TrackPropertyValue(
                enableAnimationDebuggingProperty,
                property =>
                {
                    debugAnimationTimeElement.SetEnabled(property.boolValue);
                }
            );
        }
    }
}
