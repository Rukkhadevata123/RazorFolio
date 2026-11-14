using System.Collections;
using System.ComponentModel;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.Utilities;

namespace RazorFolio.Core.Serialization;

internal sealed class FrontMatterValueDeserializer(IValueDeserializer inner) : IValueDeserializer
{
    public static IDeserializer Create()
    {
        var values = new StaticDeserializerBuilder(new RazorFolioYamlContext())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithDuplicateKeyChecking()
            .BuildValueDeserializer();

        return Deserializer.FromValueDeserializer(new FrontMatterValueDeserializer(values));
    }

    public object? DeserializeValue(
        IParser parser, Type expectedType, SerializerState state, IValueDeserializer nestedObjectDeserializer)
    {
        var current = parser.Current;
        var start = current?.Start ?? Mark.Empty;
        var end = current?.End ?? Mark.Empty;
        var value = inner.DeserializeValue(parser, expectedType, state, nestedObjectDeserializer);

        try
        {
            // The inner alias handler stores values before our conversions. Later aliases must see the converted value.
            var anchors = state.Get<ConvertedAnchors>();
            if (current is AnchorAlias alias && anchors.TryGetValue(alias.Value, out var anchoredValue))
            {
                value = anchoredValue;
            }

            var converted = value is IValuePromise promise
                ? new ConvertedPromise(promise, expectedType)
                : ConvertValue(value, expectedType);

            if (current is NodeEvent { Anchor.IsEmpty: false } node)
            {
                anchors[node.Anchor] = converted;
            }

            return converted;
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            throw new YamlException(start, end, "Exception during deserialization", exception);
        }
    }

    private static object? ConvertValue(object? value, Type expectedType)
    {
        // Static builders omit the reflection builder's conversions. Keep only those needed by front matter.
        if (value is null)
        {
            return expectedType == typeof(bool) ? false : null;
        }

        if (expectedType.IsInstanceOfType(value))
        {
            return value;
        }

        if (expectedType == typeof(string))
        {
            // Select known converters directly; TypeDescriptor would discover them through reflection.
            return value switch
            {
                DateTime date => new DateTimeConverter().ConvertToInvariantString(date),
                ICollection collection => new CollectionConverter().ConvertToInvariantString(collection),
                _ => new System.ComponentModel.TypeConverter().ConvertToInvariantString(value)
            };
        }

        if (expectedType == typeof(bool))
        {
            return value switch
            {
                "0" => false,
                "1" => true,
                _ => ((IConvertible)value).ToBoolean(CultureInfo.InvariantCulture)
            };
        }

        throw new InvalidCastException($"Cannot convert a YAML value to {expectedType.Name}.");
    }

    private sealed class ConvertedAnchors : Dictionary<AnchorName, object?>;

    private sealed class ConvertedPromise : IValuePromise
    {
        public event Action<object?>? ValueAvailable;

        public ConvertedPromise(IValuePromise inner, Type expectedType)
        {
            inner.ValueAvailable += value => ValueAvailable?.Invoke(ConvertValue(value, expectedType));
        }
    }
}
