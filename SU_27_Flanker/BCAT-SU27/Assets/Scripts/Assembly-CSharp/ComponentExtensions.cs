using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public static class ComponentExtensions
{
	private static FieldInfo UnityEventBase_m_Calls;

	private static Dictionary<Type, PropertyInfo> invokeCallList_Count = new Dictionary<Type, PropertyInfo>();

	public static T GetComponentImplementing<T>(this GameObject go)
	{
		Component[] components = go.GetComponents<Component>();
		foreach (Component component in components)
		{
			if (typeof(T).IsAssignableFrom(component.GetType()))
			{
				return (T)(object)component;
			}
		}
		return default(T);
	}

	public static T[] GetComponentsImplementing<T>(this GameObject go)
	{
		return (from x in go.GetComponents<Component>()
			where typeof(T).IsAssignableFrom(x.GetType())
			select x).Cast<T>().ToArray();
	}

	public static T[] GetComponentsInChildrenImplementing<T>(this GameObject go, bool includeInactive = false)
	{
		return (from x in go.GetComponentsInChildren<Component>(includeInactive)
			where (bool)x && typeof(T).IsAssignableFrom(x.GetType())
			select x).Cast<T>().ToArray();
	}

	public static T[] GetComponentsInParentImplementing<T>(this GameObject go)
	{
		return (from x in go.GetComponentsInParent<Component>()
			where typeof(T).IsAssignableFrom(x.GetType())
			select x).Cast<T>().ToArray();
	}

	public static T GetComponentInParentImplementing<T>(this GameObject go)
	{
		T[] componentsInParentImplementing = go.GetComponentsInParentImplementing<T>();
		int num = 0;
		if (num < componentsInParentImplementing.Length)
		{
			return componentsInParentImplementing[num];
		}
		return default(T);
	}

	public static T GetComponentInChildrenImplementing<T>(this GameObject go, bool includeInactive = false)
	{
		T[] componentsInChildrenImplementing = go.GetComponentsInChildrenImplementing<T>(includeInactive);
		int num = 0;
		if (num < componentsInChildrenImplementing.Length)
		{
			return componentsInChildrenImplementing[num];
		}
		return default(T);
	}

	public static void SetActive(this GameObject[] objects, bool active)
	{
		for (int i = 0; i < objects.Length; i++)
		{
			if ((bool)objects[i])
			{
				objects[i].SetActive(active);
			}
		}
	}

	public static T Random<T>(this T[] array, int start = 0, int end = -1)
	{
		if (end < 0)
		{
			end = array.Length;
		}
		return array[UnityEngine.Random.Range(start, end)];
	}

	public static bool HasListeners(this UnityEventBase unityEvent)
	{
		if (unityEvent == null)
		{
			return false;
		}
		if (unityEvent.GetPersistentEventCount() > 0)
		{
			return true;
		}
		if (UnityEventBase_m_Calls == null)
		{
			UnityEventBase_m_Calls = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
		}
		object value = UnityEventBase_m_Calls.GetValue(unityEvent);
		Type type = value.GetType();
		if (!invokeCallList_Count.TryGetValue(type, out var value2))
		{
			value2 = value.GetType().GetProperty("Count");
			invokeCallList_Count.Add(type, value2);
		}
		return (int)value2.GetValue(value) > 0;
	}
}
