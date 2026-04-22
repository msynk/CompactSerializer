using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace CompactBinarySerializer;

public static class CompactBinarySerializer
{
    private static readonly ConcurrentDictionary<Type, TypeContract> TypeContracts = new();

    public static byte[] Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var writer = new CompactWriter();
        WriteValue(writer, typeof(T), value);
        return writer.ToArray();
    }

    public static T Deserialize<T>(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            throw new ArgumentException("Payload cannot be empty.", nameof(payload));
        }

        var reader = new CompactReader(payload);
        var value = (T?)ReadValue(reader, typeof(T));
        if (value is null)
        {
            throw new InvalidOperationException("Deserialization produced null for a non-nullable root type.");
        }

        return value;
    }

    private static void WriteValue(CompactWriter writer, Type type, object? value)
    {
        if (IsNullable(type))
        {
            if (value is null)
            {
                writer.WriteByte(0);
                return;
            }

            writer.WriteByte(1);
            var underlying = Nullable.GetUnderlyingType(type)!;
            WriteValue(writer, underlying, value);
            return;
        }

        if (!type.IsValueType)
        {
            if (value is null)
            {
                writer.WriteByte(0);
                return;
            }

            writer.WriteByte(1);
        }

        if (type == typeof(string))
        {
            writer.WriteString((string)value!);
            return;
        }

        if (type == typeof(bool))
        {
            writer.WriteByte((bool)value! ? (byte)1 : (byte)0);
            return;
        }

        if (type == typeof(byte))
        {
            writer.WriteByte((byte)value!);
            return;
        }

        if (type == typeof(short))
        {
            writer.WriteInt32((short)value!);
            return;
        }

        if (type == typeof(int))
        {
            writer.WriteInt32((int)value!);
            return;
        }

        if (type == typeof(long))
        {
            writer.WriteInt64((long)value!);
            return;
        }

        if (type == typeof(ushort))
        {
            writer.WriteUInt32((ushort)value!);
            return;
        }

        if (type == typeof(uint))
        {
            writer.WriteUInt32((uint)value!);
            return;
        }

        if (type == typeof(ulong))
        {
            writer.WriteUInt64((ulong)value!);
            return;
        }

        if (type == typeof(float))
        {
            writer.WriteSingle((float)value!);
            return;
        }

        if (type == typeof(double))
        {
            writer.WriteDouble((double)value!);
            return;
        }

        if (type == typeof(decimal))
        {
            writer.WriteDecimal((decimal)value!);
            return;
        }

        if (type == typeof(DateTime))
        {
            writer.WriteInt64(((DateTime)value!).ToBinary());
            return;
        }

        if (type == typeof(Guid))
        {
            writer.WriteBytes(((Guid)value!).ToByteArray());
            return;
        }

        if (type.IsEnum)
        {
            var enumValue = Convert.ToInt64(value);
            writer.WriteInt64(enumValue);
            return;
        }

        if (type == typeof(byte[]))
        {
            writer.WriteByteArray((byte[])value!);
            return;
        }

        if (TryWriteEnumerable(writer, type, value))
        {
            return;
        }

        var contract = GetOrCreateTypeContract(type);
        foreach (var member in contract.Members)
        {
            var propertyValue = member.Getter(value!);
            WriteValue(writer, member.PropertyType, propertyValue);
        }
    }

    private static object? ReadValue(CompactReader reader, Type type)
    {
        if (IsNullable(type))
        {
            var exists = reader.ReadByte();
            if (exists == 0)
            {
                return null;
            }

            var underlying = Nullable.GetUnderlyingType(type)!;
            var underlyingValue = ReadValue(reader, underlying);
            return Activator.CreateInstance(type, underlyingValue);
        }

        if (!type.IsValueType)
        {
            var exists = reader.ReadByte();
            if (exists == 0)
            {
                return null;
            }
        }

        if (type == typeof(string))
        {
            return reader.ReadString();
        }

        if (type == typeof(bool))
        {
            return reader.ReadByte() == 1;
        }

        if (type == typeof(byte))
        {
            return reader.ReadByte();
        }

        if (type == typeof(short))
        {
            return (short)reader.ReadInt32();
        }

        if (type == typeof(int))
        {
            return reader.ReadInt32();
        }

        if (type == typeof(long))
        {
            return reader.ReadInt64();
        }

        if (type == typeof(ushort))
        {
            return (ushort)reader.ReadUInt32();
        }

        if (type == typeof(uint))
        {
            return reader.ReadUInt32();
        }

        if (type == typeof(ulong))
        {
            return reader.ReadUInt64();
        }

        if (type == typeof(float))
        {
            return reader.ReadSingle();
        }

        if (type == typeof(double))
        {
            return reader.ReadDouble();
        }

        if (type == typeof(decimal))
        {
            return reader.ReadDecimal();
        }

        if (type == typeof(DateTime))
        {
            return DateTime.FromBinary(reader.ReadInt64());
        }

        if (type == typeof(Guid))
        {
            var bytes = reader.ReadBytes(16);
            return new Guid(bytes);
        }

        if (type.IsEnum)
        {
            var enumValue = reader.ReadInt64();
            return Enum.ToObject(type, enumValue);
        }

        if (type == typeof(byte[]))
        {
            return reader.ReadByteArray();
        }

        if (TryReadEnumerable(reader, type, out var enumerableValue))
        {
            return enumerableValue;
        }

        var contract = GetOrCreateTypeContract(type);
        var instance = contract.CreateInstance();
        foreach (var member in contract.Members)
        {
            var propertyValue = ReadValue(reader, member.PropertyType);
            member.Setter(instance, propertyValue);
        }

        return instance;
    }

    private static bool TryWriteEnumerable(CompactWriter writer, Type type, object? value)
    {
        if (type.IsArray)
        {
            var array = (Array)value!;
            writer.WriteUInt32((uint)array.Length);
            var elementType = type.GetElementType()!;
            for (var i = 0; i < array.Length; i++)
            {
                WriteValue(writer, elementType, array.GetValue(i));
            }

            return true;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (IList)value!;
            writer.WriteUInt32((uint)list.Count);
            var elementType = type.GetGenericArguments()[0];
            foreach (var item in list)
            {
                WriteValue(writer, elementType, item);
            }

            return true;
        }

        return false;
    }

    private static bool TryReadEnumerable(CompactReader reader, Type type, out object? value)
    {
        if (type.IsArray)
        {
            var length = checked((int)reader.ReadUInt32());
            var elementType = type.GetElementType()!;
            var array = Array.CreateInstance(elementType, length);
            for (var i = 0; i < length; i++)
            {
                array.SetValue(ReadValue(reader, elementType), i);
            }

            value = array;
            return true;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var count = checked((int)reader.ReadUInt32());
            var elementType = type.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(type)!;
            for (var i = 0; i < count; i++)
            {
                list.Add(ReadValue(reader, elementType));
            }

            value = list;
            return true;
        }

        value = null;
        return false;
    }

    private static bool IsNullable(Type type) => Nullable.GetUnderlyingType(type) is not null;

    private static TypeContract GetOrCreateTypeContract(Type type)
    {
        return TypeContracts.GetOrAdd(type, BuildTypeContract);
    }

    private static TypeContract BuildTypeContract(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetMethod is not null && p.SetMethod is not null)
            .Select(p => new
            {
                Property = p,
                Order = p.GetCustomAttribute<CompactIndexAttribute>()?.Index,
                MetadataToken = p.MetadataToken
            })
            .OrderBy(p => p.Order ?? int.MaxValue)
            .ThenBy(p => p.MetadataToken)
            .Select(p => p.Property)
            .ToArray();

        var members = properties.Select(CreateMemberContract).ToArray();
        var ctor = CreateFactory(type);
        return new TypeContract(ctor, members);
    }

    private static MemberContract CreateMemberContract(PropertyInfo property)
    {
        var getter = CreateGetter(property);
        var setter = CreateSetter(property);
        return new MemberContract(property.PropertyType, getter, setter);
    }

    private static Func<object> CreateFactory(Type type)
    {
        var ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor is null)
        {
            throw new InvalidOperationException($"Could not construct type '{type.FullName}'. A public parameterless constructor is required.");
        }

        var newExpression = Expression.New(ctor);
        var castToObject = Expression.Convert(newExpression, typeof(object));
        return Expression.Lambda<Func<object>>(castToObject).Compile();
    }

    private static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instance, property.DeclaringType!);
        var propertyAccess = Expression.Property(typedInstance, property);
        var castToObject = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<object, object?>>(castToObject, instance).Compile();
    }

    private static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        var typedInstance = Expression.Convert(instance, property.DeclaringType!);
        var typedValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(typedInstance, property), typedValue);

        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }
}

internal sealed record TypeContract(Func<object> CreateInstance, MemberContract[] Members);

internal sealed record MemberContract(
    Type PropertyType,
    Func<object, object?> Getter,
    Action<object, object?> Setter);
