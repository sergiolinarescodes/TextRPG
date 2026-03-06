using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

namespace TextAnimationsForUIToolkit
{
    public class Parameters
    {
        [CanBeNull]
        public string mainParameter { get; private set; }

        private readonly Dictionary<string, string> _parameters = new();
        private readonly StringBuilder _rawStringBuilder = new();

        public bool TryGetValue(string key, out string value)
        {
            return _parameters.TryGetValue(key, out value);
        }

        public bool TryGetFloatValue(string key, out float value)
        {
            value = float.NaN;
            return _parameters.TryGetValue(key, out var stringValue)
                && TryParseFloat(stringValue, out value);
        }

        public bool TryGetIntValue(string key, out int value)
        {
            value = 0;
            return _parameters.TryGetValue(key, out var stringValue)
                && TryParseInt(stringValue, out value);
        }

        public bool TryGetColorValue(string key, out Color value)
        {
            value = default;
            return _parameters.TryGetValue(key, out var stringValue)
                && TryParseColor(stringValue, out value);
        }

        public bool TryGetMainValue(out string value)
        {
            value = mainParameter;
            return mainParameter != null;
        }

        public bool TryGetMainFloatValue(out float value, bool canBePercent = false)
        {
            value = float.NaN;
            return mainParameter != null && TryParseFloat(mainParameter, out value, canBePercent);
        }

        public bool TryGetMainIntValue(out int value)
        {
            value = 0;
            return mainParameter != null && TryParseInt(mainParameter, out value);
        }

        public bool TryGetMainColorValue(out Color value)
        {
            value = default;
            return mainParameter != null && TryParseColor(mainParameter, out value);
        }

        public static bool TryParseFloat(string stringValue, out float value, bool canBePercent = false)
        {
            var isPercent = false;
            if (canBePercent && stringValue.EndsWith('%'))
            {
                stringValue = stringValue[..^1];
                isPercent = true;
            }

            var wasSuccessful = float.TryParse(
                stringValue,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value
            );

            if (!wasSuccessful)
            {
                return false;
            }

            if (isPercent)
            {
                value /= 100f;
            }

            return true;
        }

        private static bool TryParseInt(string stringValue, out int value)
        {
            return int.TryParse(
                stringValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        private bool TryParseColor(string color, out Color value)
        {
            switch (color)
            {
                case "red":
                    value = Color.red;
                    break;
                case "green":
                    value = Color.green;
                    break;
                case "blue":
                    value = Color.blue;
                    break;
                case "white":
                    value = Color.white;
                    break;
                case "black":
                    value = Color.black;
                    break;
                case "grey":
                    value = Color.grey;
                    break;
                case var _:
                    return TryParseHexColor(color, out value);
            }

            return true;
        }

        private static bool TryParseHexColor(string hex, out Color value)
        {
            value = default;

            if (!hex.StartsWith("#"))
            {
                return false;
            }

            if (hex.Length != 7 && hex.Length != 9)
            {
                return false;
            }

            var r = Convert.ToByte(hex.Substring(1, 2), 16);
            var g = Convert.ToByte(hex.Substring(3, 2), 16);
            var b = Convert.ToByte(hex.Substring(5, 2), 16);

            var a = (byte)255;
            if (hex.Length == 9)
            {
                a = Convert.ToByte(hex.Substring(7, 2), 16);
            }

            value = new Color32(r, g, b, a);
            return true;
        }

        /// <summary>
        /// Set the main parameter and appends it to the raw string.
        ///
        /// Must be called before Add is called the first time.
        /// Must only be called once.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="rawString"></param>
        internal void AddMainParameter(string value, string rawString)
        {
            _rawStringBuilder.Append('=');
            _rawStringBuilder.Append(rawString);

            mainParameter = value;
        }

        internal void Add(string key, string value, string rawString)
        {
            _parameters.Add(key, value);
            _rawStringBuilder.Append(" ");
            _rawStringBuilder.Append(key);
            _rawStringBuilder.Append("=");
            _rawStringBuilder.Append(rawString);
        }

        public override string ToString()
        {
            return _rawStringBuilder.ToString();
        }

        public void BuildString(StringBuilder builder)
        {
            builder.Append(_rawStringBuilder);
        }
    }
}
