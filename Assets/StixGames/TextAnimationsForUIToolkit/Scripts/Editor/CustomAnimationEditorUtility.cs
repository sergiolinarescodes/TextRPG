using System.IO;
using TextAnimationsForUIToolkit.CustomAnimations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextAnimationsForUIToolkit
{
    public static class CustomAnimationEditorUtility
    {
        public static void SetupCreateCustomAnimationButton(
            VisualElement customInspector,
            SerializedObject serializedObject,
            bool setApperanceAndVanishing
        )
        {
            var createCustomAnimation = customInspector.Q<Button>("create-custom-animation");
            var customAnimations = customInspector.Q<PropertyField>("custom-animations");
            var customAnimationsProperty = serializedObject.FindProperty(
                customAnimations.bindingPath
            );
            createCustomAnimation.RegisterCallback<
                ClickEvent,
                (SerializedObject, SerializedProperty, bool)
            >(
                OnCreateCustomAnimationButtonClicked,
                (serializedObject, customAnimationsProperty, setApperanceAndVanishing)
            );
        }

        private static void OnCreateCustomAnimationButtonClicked(
            ClickEvent evt,
            (SerializedObject, SerializedProperty, bool) data
        )
        {
            var (serializedObject, property, setApperanceAndVanishing) = data;

            var path = EditorUtility.SaveFilePanel(
                "Create custom animation preset",
                "Assets",
                "Custom Animation Preset",
                "asset"
            );
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);

            var instance = ScriptableObject.CreateInstance<CustomAnimationPreset>();
            instance.isTextAppearanceEffect = setApperanceAndVanishing;
            instance.isTextVanishingEffect = setApperanceAndVanishing;
            AssetDatabase.CreateAsset(instance, path);

            var index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            var indexProperty = property.GetArrayElementAtIndex(index);
            indexProperty.objectReferenceValue = instance;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
