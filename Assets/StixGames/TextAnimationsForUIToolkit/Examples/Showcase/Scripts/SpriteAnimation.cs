using UnityEngine;

namespace StixGames.TextAnimationsForUIToolkit.Examples.Showcase.Scripts
{
    public class SpriteAnimation : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public float framesPerSecond = 4;
        public Sprite[] sprites;
        public bool disableAfterFirstCycle;

        private int _currentSprite;
        private float _animationTime;

        private void OnEnable()
        {
            spriteRenderer.sprite = sprites[0];
            _currentSprite = 0;
            _animationTime = 0;
        }

        private void Update()
        {
            _animationTime += Time.deltaTime * framesPerSecond;
            if (_animationTime < 1)
            {
                return;
            }

            _animationTime = 0;
            _currentSprite++;

            if (_currentSprite >= sprites.Length && disableAfterFirstCycle)
            {
                gameObject.SetActive(false);
            }

            _currentSprite %= sprites.Length;
            spriteRenderer.sprite = sprites[_currentSprite];
        }
    }
}
