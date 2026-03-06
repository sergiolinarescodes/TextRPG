using System;
using TextAnimationsForUIToolkit.Animations;
using System.Collections.Generic;
using System.Linq;
using TextAnimationsForUIToolkit.CustomAnimations;

namespace TextAnimationsForUIToolkit
{
    [Serializable]
    public class DefaultAnimations
    {
        public bool useBounce;
        public Bounce bounce = new();

        public bool useRainbow;
        public Rainbow rainbow = new();

        public bool useRotate;
        public Rotate rotate = new();

        public bool useShake;
        public Shake shake = new();

        public bool useSizeWave;
        public SizeWave sizeWave = new();

        public bool useSwing;
        public Swing swing = new();

        public bool useWave;
        public Wave wave = new();

        public bool useWiggle;
        public Wiggle wiggle = new();

        public List<CustomAnimationPreset> customAnimations = new();

        public List<TextAnimation> GetAnimations()
        {
            var animations = new List<TextAnimation>();

            if (useBounce)
            {
                animations.Add(bounce);
            }

            if (useRainbow)
            {
                animations.Add(rainbow);
            }

            if (useRotate)
            {
                animations.Add(rotate);
            }

            if (useShake)
            {
                animations.Add(shake);
            }

            if (useSizeWave)
            {
                animations.Add(sizeWave);
            }

            if (useSwing)
            {
                animations.Add(swing);
            }

            if (useWave)
            {
                animations.Add(wave);
            }

            if (useWiggle)
            {
                animations.Add(wiggle);
            }

            animations.AddRange(customAnimations
                .Select(preset => new CustomAnimation(null, preset)
                {
                    frequency = preset.defaultFrequency,
                    waveSize = preset.defaultWaveSize,
                }));

            return animations;
        }
    }
}