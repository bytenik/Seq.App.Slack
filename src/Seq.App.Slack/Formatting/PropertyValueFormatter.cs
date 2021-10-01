using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Seq.App.Slack.Formatting
{
    class PropertyValueFormatter
    {
        private readonly int? _maxPropertyLength;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public PropertyValueFormatter(int? maxPropertyLength)
        {
            _maxPropertyLength = maxPropertyLength;
        }

        public string ConvertPropertyValueToString(object propertyValue)
        {
            if (propertyValue == null)
                return string.Empty;

            string result;
            Type t = propertyValue.GetType();
            bool isDict = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            if (isDict)
            {
                result = JsonConvert.SerializeObject(propertyValue, JsonSettings);
            }
            else
            {
                result = propertyValue.ToString();
            }

            if (_maxPropertyLength.HasValue)
            {
                if (result.Length > _maxPropertyLength)
                {
                    result = result.Substring(0, _maxPropertyLength.Value) + "...";
                }
            }

            return result;
        }
    }
}