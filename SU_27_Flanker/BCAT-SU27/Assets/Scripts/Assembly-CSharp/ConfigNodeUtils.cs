using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class ConfigNodeUtils
{
	private const string cultureString = "en-US";

	private const string numberFormat = "G";

	private static CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

	public static string WriteObject(object o)
	{
		if (o == null)
		{
			Debug.LogError("Attempted to write null with no type parameter.");
			return string.Empty;
		}
		return WriteObject(o.GetType(), o);
	}

	public static string WriteObject(Type type, object o)
	{
		if (type == typeof(string))
		{
			if (string.IsNullOrEmpty((string)o))
			{
				return string.Empty;
			}
			return (string)o;
		}
		if (type == typeof(bool) || type.IsEnum)
		{
			return o.ToString();
		}
		if (type == typeof(int))
		{
			return ((int)o).ToString("G", culture);
		}
		if (type == typeof(float))
		{
			float f = (float)o;
			if (float.IsNaN(f))
			{
				Debug.LogError("ConfigNodeUtils: Attempted to save a NaN float!");
			}
			return WriteFloat(f);
		}
		if (typeof(IConfigValue).IsAssignableFrom(type))
		{
			return ((IConfigValue)o).WriteValue();
		}
		if (type == typeof(double))
		{
			double d = (double)o;
			if (double.IsNaN(d))
			{
				Debug.LogError("ConfigNodeUtils: Attempted to save a NaN double!");
			}
			return WriteDouble(d);
		}
		if (type == typeof(Vector3))
		{
			return WriteVector3((Vector3)o);
		}
		if (type == typeof(IntVector2))
		{
			return WriteIntVector2((IntVector2)o);
		}
		if (type == typeof(Vector3D))
		{
			return WriteVector3D((Vector3D)o);
		}
		if (type == typeof(Color))
		{
			return WriteColor((Color)o);
		}
		if (type == typeof(Quaternion))
		{
			return WriteQuaternion((Quaternion)o);
		}
		if (o is IList)
		{
			return WriteList((IList)o);
		}
		Debug.Log("Unhandled confignode object type: " + type.ToString());
		return string.Empty;
	}

	public static object ParseObject(Type type, string s)
	{
		if (typeof(IConfigValue).IsAssignableFrom(type))
		{
			object obj = InstantiateConfigValue(type);
			((IConfigValue)obj).ConstructFromValue(s);
			return obj;
		}
		if (type == typeof(float))
		{
			return ParseFloat(s);
		}
		if (type == typeof(int))
		{
			return ParseInt(s);
		}
		if (type == typeof(Vector3))
		{
			return ParseVector3(s);
		}
		if (type == typeof(Vector3D))
		{
			return ParseVector3D(s);
		}
		if (type.IsEnum)
		{
			return ParseEnum(type, s);
		}
		if (type == typeof(string))
		{
			return s;
		}
		if (type == typeof(bool))
		{
			return ParseBool(s);
		}
		if (type == typeof(IntVector2))
		{
			return ParseIntVector2(s);
		}
		if (type == typeof(double))
		{
			return ParseDouble(s);
		}
		if (type == typeof(Color))
		{
			return ParseColor(s);
		}
		if (typeof(IList).IsAssignableFrom(type))
		{
			return ParseList(type.GetGenericArguments()[0], s);
		}
		if (type == typeof(Quaternion))
		{
			return ParseQuaternion(s);
		}
		Debug.LogError("ParseObject - Invalid config node value type: " + type.ToString());
		return null;
	}

	private static object InstantiateConfigValue(Type type)
	{
		return Activator.CreateInstance(type);
	}

	public static T ParseObject<T>(string s)
	{
		return (T)ParseObject(typeof(T), s);
	}

	public static string WriteQuaternion(Quaternion q)
	{
		return WriteVector3(q.eulerAngles);
	}

	public static Quaternion ParseQuaternion(string s)
	{
		return Quaternion.Euler(ParseVector3(s));
	}

	public static float ParseFloat(string s)
	{
		if (float.TryParse(s, NumberStyles.Number | NumberStyles.AllowExponent, culture, out var result))
		{
			return result;
		}
		Debug.LogError("Failed to parse float '" + s + "'.");
		return 0f;
	}

	public static bool ParseBool(string s)
	{
		if (bool.TryParse(s, out var result))
		{
			return result;
		}
		Debug.LogError("Failed to parse bool '" + s + "'.");
		return false;
	}

	public static int ParseInt(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0;
		}
		if (int.TryParse(s, out var result))
		{
			return result;
		}
		Debug.LogError("Failed to parse int'" + s + "'.");
		return 0;
	}

	public static string WriteVector3(Vector3 v)
	{
		if (float.IsNaN(v.x))
		{
			Debug.LogError("ConfigNodeUtils: Attempted to save a NaN Vector3 coordinate!");
		}
		return "(" + WriteFloat(v.x) + ", " + WriteFloat(v.y) + ", " + WriteFloat(v.z) + ")";
	}

	private static string WriteFloat(float f)
	{
		return f.ToString("G", culture);
	}

	public static Vector3 ParseVector3(string s)
	{
		s = s.Replace("(", string.Empty).Replace(")", string.Empty).Replace(" ", string.Empty)
			.Trim();
		string[] array = s.Split(',');
		return new Vector3(ParseFloat(array[0]), ParseFloat(array[1]), ParseFloat(array[2]));
	}

	public static string WriteVector3D(Vector3D v)
	{
		if (double.IsNaN(v.x))
		{
			Debug.LogError("ConfigNodeUtils: Attempted to save a NaN Vector3D coordinate!");
		}
		return "(" + WriteDouble(v.x) + ", " + WriteDouble(v.y) + ", " + WriteDouble(v.z) + ")";
	}

	private static string WriteDouble(double d)
	{
		return d.ToString("G17", culture);
	}

	private static double ParseDouble(string s)
	{
		if (double.TryParse(s, NumberStyles.Number | NumberStyles.AllowExponent, culture, out var result))
		{
			return result;
		}
		Debug.LogError("Failed to parse double '" + s + "'.");
		return 0.0;
	}

	public static Vector3D ParseVector3D(string s)
	{
		s = s.Replace("(", string.Empty).Replace(")", string.Empty).Replace(" ", string.Empty)
			.Trim();
		string[] array = s.Split(',');
		return new Vector3D(ParseDouble(array[0]), ParseDouble(array[1]), ParseDouble(array[2]));
	}

	public static List<string> ParseList(string s)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(s))
		{
			string[] array = s.Split(';');
			for (int i = 0; i < array.Length - 1; i++)
			{
				list.Add(array[i]);
			}
		}
		return list;
	}

	public static List<T> ParseList<T>(string s, int startIdx = 0, int endIdx = -1)
	{
		List<T> list = new List<T>();
		string[] array = s.Split(';');
		endIdx = ((endIdx >= 0) ? Mathf.Min(endIdx, array.Length - 1) : (array.Length - 1));
		for (int i = startIdx; i < endIdx; i++)
		{
			list.Add(ParseObject<T>(array[i]));
		}
		return list;
	}

	public static object ParseList(Type type, string s, int startIdx = 0, int endIdx = -1)
	{
		IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
		string[] array = s.Split(';');
		endIdx = ((endIdx >= 0) ? Mathf.Min(endIdx, array.Length - 1) : (array.Length - 1));
		for (int i = startIdx; i < endIdx; i++)
		{
			list.Add(ParseObject(type, array[i]));
		}
		return list;
	}

	public static string WriteList(IList list)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (object item in list)
		{
			stringBuilder.Append(WriteObject(item));
			stringBuilder.Append(';');
		}
		return stringBuilder.ToString();
	}

	public static string WriteList(List<string> list)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in list)
		{
			stringBuilder.Append(item);
			stringBuilder.Append(';');
		}
		return stringBuilder.ToString();
	}

	public static string WriteList<T>(List<T> list)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (T item in list)
		{
			stringBuilder.Append(WriteObject(item));
			stringBuilder.Append(';');
		}
		return stringBuilder.ToString();
	}

	public static T ParseEnum<T>(string s)
	{
		try
		{
			return (T)Enum.Parse(typeof(T), s);
		}
		catch (ArgumentException)
		{
			return (T)Enum.GetValues(typeof(T)).GetValue(0);
		}
	}

	public static object ParseEnum(Type t, string s)
	{
		try
		{
			return Enum.Parse(t, s);
		}
		catch (ArgumentException)
		{
			return Enum.GetValues(t).GetValue(0);
		}
	}

	public static string WriteIntVector2(IntVector2 vector)
	{
		return $"({vector.x},{vector.y})";
	}

	public static IntVector2 ParseIntVector2(string s)
	{
		string[] array = s.Substring(1, s.Length - 2).Split(',');
		return new IntVector2(int.Parse(array[0]), int.Parse(array[1]));
	}

	public static string WriteColor(Color color)
	{
		return "(" + color.r + ", " + color.g + ", " + color.b + ", " + color.a + ")";
	}

	public static Color ParseColor(string s)
	{
		s = s.Replace("(", string.Empty).Replace(")", string.Empty).Replace(" ", string.Empty)
			.Trim();
		string[] array = s.Split(',');
		return new Color(ParseFloat(array[0]), ParseFloat(array[1]), ParseFloat(array[2]), ParseFloat(array[3]));
	}

	public static string WriteMatrix(Matrix4x4 m)
	{
		return WriteList(new List<float>
		{
			m.m00, m.m01, m.m02, m.m03, m.m10, m.m11, m.m12, m.m13, m.m20, m.m21,
			m.m22, m.m23, m.m30, m.m31, m.m32, m.m33
		});
	}

	public static Matrix4x4 ParseMatrix(string s)
	{
		List<float> list = ParseList<float>(s);
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = list[0];
		result.m01 = list[1];
		result.m02 = list[2];
		result.m03 = list[3];
		result.m10 = list[4];
		result.m11 = list[5];
		result.m12 = list[6];
		result.m13 = list[7];
		result.m20 = list[8];
		result.m21 = list[9];
		result.m22 = list[10];
		result.m23 = list[11];
		result.m30 = list[12];
		result.m31 = list[13];
		result.m32 = list[14];
		result.m33 = list[15];
		return result;
	}

	public static bool TryParseValue<T>(ConfigNode node, string name, ref T target)
	{
		if (node.HasValue(name))
		{
			target = ParseObject<T>(node.GetValue(name));
			return true;
		}
		return false;
	}

	public static string SanitizeInputStringStrict(string s)
	{
		s = s.Replace(";", string.Empty).Replace("{", string.Empty).Replace("}", string.Empty)
			.Replace(",", string.Empty)
			.Replace("=", string.Empty)
			.Replace("(", string.Empty)
			.Replace(")", string.Empty)
			.Replace(":", string.Empty)
			.Replace("/", string.Empty)
			.Replace("\\", string.Empty)
			.Trim();
		return s;
	}

	public static string SanitizeInputString(string s)
	{
		s = s.Replace("{", "(").Replace("}", ")").Trim();
		return s;
	}
}
