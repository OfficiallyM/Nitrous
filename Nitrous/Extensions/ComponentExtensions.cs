using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nitrous.Extensions
{
	// Source: Kuri_AttachEverything.
	internal static class ComponentExtensions
	{
		public static T GetCopyOf<T>(this Component comp, T other) where T : Component
		{
			Type type = comp.GetType();
			if (type != other.GetType())
				return default(T);
			List<Type> typeList = new List<Type>();
			for (Type baseType = type.BaseType; baseType != null && !(baseType == typeof(MonoBehaviour)); baseType = baseType.BaseType)
				typeList.Add(baseType);
			IEnumerable<PropertyInfo> propertyInfos = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (Type type1 in typeList)
				propertyInfos = propertyInfos.Concat(type1.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
			IEnumerable<PropertyInfo> source = propertyInfos.Where(property => !(type == typeof(Rigidbody)) || !(property.Name == "inertiaTensor")).Where(property => !property.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ObsoleteAttribute)));
			foreach (PropertyInfo propertyInfo in source)
			{
				PropertyInfo pinfo = propertyInfo;
				if (pinfo.CanWrite)
				{
					if (!source.Any(e => e.Name == string.Format("shared{0}{1}", char.ToUpper(pinfo.Name[0]), pinfo.Name.Substring(1))))
					{
						try
						{
							pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
						}
						catch
						{
						}
					}
				}
			}
			IEnumerable<FieldInfo> fieldInfos = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fieldInfos)
			{
				FieldInfo finfo = fieldInfo;
				foreach (Type type2 in typeList)
				{
					if (!fieldInfos.Any(e => e.Name == string.Format("shared{0}{1}", char.ToUpper(finfo.Name[0]), finfo.Name.Substring(1))))
						fieldInfos = fieldInfos.Concat(type2.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
				}
			}
			foreach (FieldInfo fieldInfo in fieldInfos)
				fieldInfo.SetValue(comp, fieldInfo.GetValue(other));
			foreach (FieldInfo fieldInfo in fieldInfos.Where(field => field.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ObsoleteAttribute))))
				fieldInfo.SetValue(comp, fieldInfo.GetValue(other));
			return comp as T;
		}

		public static T CopyComponent<T>(this GameObject go, T toAdd) where T : Component => go.AddComponent(toAdd.GetType()).GetCopyOf(toAdd);
	}
}
