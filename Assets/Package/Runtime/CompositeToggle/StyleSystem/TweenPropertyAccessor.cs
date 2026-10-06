using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobcast.Coffee.Toggles
{
	/// <summary>Only readable properties with continuous values can be animated. Never calls arbitrary actions.</summary>
	internal sealed class TweenPropertyAccessor
	{
		internal readonly MethodInfo setter;
		readonly MethodInfo getter;
		readonly Type valueType;

		TweenPropertyAccessor(PropertyInfo property)
		{
			setter = property.GetSetMethod();
			getter = property.GetGetMethod();
			valueType = property.PropertyType;
		}

		internal static TweenPropertyAccessor Create(MethodInfo setter, Type valueType)
		{
			if (setter == null || !setter.IsSpecialName || !setter.Name.StartsWith("set_", StringComparison.Ordinal)
				|| (valueType != typeof(float) && valueType != typeof(Vector2) && valueType != typeof(Vector3)
					&& valueType != typeof(Vector4) && valueType != typeof(Color)))
				return null;

			foreach (var property in setter.DeclaringType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (property.GetIndexParameters().Length == 0 && property.GetGetMethod() != null
					&& SameSetter(property.GetSetMethod(), setter) && property.PropertyType == valueType)
					return new TweenPropertyAccessor(property);
			}
			return null;
		}

		internal static bool SameSetter(MethodInfo a, MethodInfo b)
		{
			// ReflectedType differs when an inherited property is discovered via RectTransform vs Transform.
			return a != null && b != null && a.Module == b.Module && a.MetadataToken == b.MetadataToken;
		}

		internal Vector4 Read(Object target) { return Pack(getter.Invoke(target, null)); }

		internal Vector4 Pack(object value)
		{
			if (valueType == typeof(float)) return new Vector4((float)value, 0, 0, 0);
			if (valueType == typeof(Vector2)) { var v = (Vector2)value; return new Vector4(v.x, v.y, 0, 0); }
			if (valueType == typeof(Vector3)) { var v = (Vector3)value; return new Vector4(v.x, v.y, v.z, 0); }
			if (valueType == typeof(Color)) { var c = (Color)value; return new Vector4(c.r, c.g, c.b, c.a); }
			return (Vector4)value;
		}

		internal void Write(Object target, Vector4 value, object[] arguments)
		{
			if (valueType == typeof(float)) arguments[0] = value.x;
			else if (valueType == typeof(Vector2)) arguments[0] = new Vector2(value.x, value.y);
			else if (valueType == typeof(Vector3)) arguments[0] = new Vector3(value.x, value.y, value.z);
			else if (valueType == typeof(Color)) arguments[0] = new Color(value.x, value.y, value.z, value.w);
			else arguments[0] = value;
			try { setter.Invoke(target, arguments); }
			finally { arguments[0] = null; }
		}
	}
}
