﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnitsNet.Serialization.SystemTextJson
{
    /// <summary>
    /// System.Text.Json converter for a specific IQuantity subtypes.
    /// Normally, <see cref="UnitsNetIQuantityJsonConverterFactory"/> should be used from client code.
    /// </summary>
    public class UnitsNetIQuantityJsonConverter<TQuantity> : JsonConverter<TQuantity> where TQuantity : IQuantity
    {
        private readonly QuantityConverter _baseConverter;

        /// <summary>
        /// Instantiates this converter with an optional base converter
        /// </summary>
        /// <param name="baseConverter"></param>
        public UnitsNetIQuantityJsonConverter(QuantityConverter baseConverter = null)
        {
            _baseConverter = baseConverter ?? new QuantityConverter();
        }

        /// <inheritdoc cref="QuantityConverter.RegisterCustomType"/>
        public void RegisterCustomType(Type quantity, Type unit)
        {
            _baseConverter.RegisterCustomType(quantity, unit);
        }

        /// <inheritdoc cref="JsonConverter{TUnitType}.Read"/>
        public override TQuantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var token = JsonDocument.ParseValue(ref reader).RootElement;

            if (reader.TokenType == JsonTokenType.Null)
            {
                return default(TQuantity);
            }

            var valueUnit = ReadValueUnit(token);

            return (TQuantity)_baseConverter.ConvertValueUnit(valueUnit);
        }

        /// <inheritdoc cref="JsonConverter{TUnitType}.Write"/>
        public override void Write(Utf8JsonWriter writer, TQuantity value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // convert to object so that Serialize correctly used ExtendedValueUnit if necessary
            var valueUnit = (object) _baseConverter.ConvertIQuantity(value, CreateValueUnit, CreateExtendedValueUnit);
            JsonSerializer.Serialize(writer, valueUnit, options);
        }

        /// <inheritdoc cref="JsonConverter{TUnitType}.CanConvert"/>
        public override bool CanConvert(Type typeToConvert) => typeof(TQuantity).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// Factory method to create a <see cref="ValueUnit"/>
        /// </summary>
        private static ValueUnit CreateValueUnit(string unit, double value) => new ValueUnit { Unit = unit, Value = value };

        /// <summary>
        /// Factory method to create a <see cref="ExtendedValueUnit"/>
        /// </summary>
        private static ExtendedValueUnit CreateExtendedValueUnit(string unit, double value, string valueString, string valueType)
            => new ExtendedValueUnit { Unit = unit, Value = value, ValueString = valueString, ValueType = valueType};

        private ValueUnit ReadValueUnit(JsonElement serializedQuantity)
        {
            var unit = serializedQuantity.GetPropertyOrNull(nameof(ValueUnit.Unit));
            var value = serializedQuantity.GetPropertyOrNull(nameof(ValueUnit.Value));
            var valueType = serializedQuantity.GetPropertyOrNull(nameof(ExtendedValueUnit.ValueType));
            var valueString = serializedQuantity.GetPropertyOrNull(nameof(ExtendedValueUnit.ValueString));

            if (unit == null || value == null)
            {
                return null;
            }

            if (valueType == null)
            {
                if (value.Value.ValueKind != JsonValueKind.Number)
                {
                    return null;
                }

                return new ValueUnit {Unit = unit.Value.GetString(), Value = value.Value.GetDouble()};
            }

            if (valueType.Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return new ExtendedValueUnit
            {
                Unit = unit.Value.GetString(),
                Value = value.Value.GetDouble(),
                ValueType = valueType.Value.GetString(),
                ValueString = valueString?.GetString()
            };
        }

        /// <inheritdoc cref="IValueUnit"/>
        protected class ValueUnit: IValueUnit
        {
            /// <inheritdoc cref="IValueUnit.Unit"/>
            [JsonPropertyName(nameof(Unit))]
            public string Unit { get; set; }

            /// <inheritdoc cref="IValueUnit.Value"/>
            [JsonPropertyName(nameof(Value))]
            public double Value { get; set; }
        }

        /// <inheritdoc cref="IExtendedValueUnit"/>
        protected sealed class ExtendedValueUnit : ValueUnit, IExtendedValueUnit
        {
            /// <inheritdoc cref="IExtendedValueUnit.ValueString"/>
            [JsonPropertyName(nameof(ValueString))]
            public string ValueString { get; set; }

            /// <inheritdoc cref="IExtendedValueUnit.ValueType"/>
            [JsonPropertyName(nameof(ValueType))]
            public string ValueType { get; set; }
        }
    }

    static class GetPropertyOrNullExtension
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement me, string propertyName)
        {
            if (me.TryGetProperty(propertyName, out var result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}