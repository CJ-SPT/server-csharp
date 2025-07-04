using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SPTarkov.Server.Core.Utils.Json.Converters;

public class StringToNumberFactoryConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return true;
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return (JsonConverter)
            Activator.CreateInstance(
                typeof(StringToNumberConverter<>).MakeGenericType(typeToConvert)
            );
    }

    private class StringToNumberConverter<T> : JsonConverter<T>
    {
        private static readonly MethodInfo? _stringParseMethod;

        static StringToNumberConverter()
        {
            // Do reflection only once to get parse
            if (_stringParseMethod == null)
            {
                var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                _stringParseMethod = underlyingType.GetMethod(
                    "Parse",
                    [typeof(string), typeof(IFormatProvider)]
                );
            }
        }

        public override T? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();

                if (string.IsNullOrWhiteSpace(value) || value == "__REPLACEME__")
                {
                    return default;
                }

                try
                {
                    var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

                    if (_stringParseMethod != null)
                    {
                        return (T)
                            _stringParseMethod.Invoke(null, [value, CultureInfo.InvariantCulture]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"Failed to parse '{value}' into {typeToConvert.Name}, returning null."
                    );
                    return default;
                }
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return JsonSerializer.Deserialize<T>(ref reader, options);

                case JsonTokenType.Null:
                    return default;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                value = default;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
