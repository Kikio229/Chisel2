using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chisel.Framework;

public static class VertexLayoutCache
{
    static Dictionary<Type, VertexLayoutDescription> cache = new Dictionary<Type, VertexLayoutDescription>();

    public static VertexLayoutDescription Get<T>() where T : unmanaged
    {
        Type type = typeof(T);

        if (cache.TryGetValue(type, out VertexLayoutDescription layout))
        {
            return layout;
        }

        layout = Build(type);
        cache.Add(type, layout);
        return layout;
    }

    static VertexLayoutDescription Build(Type type)
    {
        List<VertexAttributeDescription> attributes = new List<VertexAttributeDescription>();

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            VertexAttributeAttribute marker = field.GetCustomAttribute<VertexAttributeAttribute>();

            if (marker == null)
            {
                continue;
            }

            attributes.Add(new VertexAttributeDescription
            {
                Location = marker.Location,
                Format = MapFormat(field.FieldType),
                Offset = (int)Marshal.OffsetOf(type, field.Name),
            });
        }

        attributes.Sort((lhs, rhs) => lhs.Location.CompareTo(rhs.Location));

        return new VertexLayoutDescription
        {
            Attributes = attributes.ToArray(),
            Stride = Marshal.SizeOf(type),
        };
    }

    static VertexElementFormat MapFormat(Type fieldType)
    {
        if (fieldType == typeof(float))
        {
            return VertexElementFormat.Float1;
        }
        if (fieldType == typeof(Vector2))
        {
            return VertexElementFormat.Float2;
        }
        if (fieldType == typeof(Vector3))
        {
            return VertexElementFormat.Float3;
        }
        if (fieldType == typeof(Vector4))
        {
            return VertexElementFormat.Float4;
        }
        if (fieldType == typeof(int))
        {
            return VertexElementFormat.Int1;
        }
        if (fieldType == typeof(uint))
        {
            return VertexElementFormat.UInt1;
        }
        if (fieldType == typeof(byte))
        {
            return VertexElementFormat.Byte1;
        }
        throw new NotSupportedException("No vertex format mapping for " + fieldType.Name);
    }
}