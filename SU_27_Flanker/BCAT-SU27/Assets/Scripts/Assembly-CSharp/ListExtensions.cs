using System.Collections.Generic;

public static class ListExtensions
{
	public static List<T> Copy<T>(this List<T> list)
	{
		if (list == null)
		{
			return null;
		}
		List<T> list2 = new List<T>();
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(list[i]);
		}
		return list2;
	}

	public static void AddOrSet<T>(this List<T> list, T item, int idx)
	{
		if (idx < list.Count)
		{
			list[idx] = item;
		}
		else
		{
			list.Add(item);
		}
	}

	public static int IndexOf<T>(this T[] arr, T obj)
	{
		if (arr != null)
		{
			for (int i = 0; i < arr.Length; i++)
			{
				if (arr[i] != null && arr[i].Equals(obj))
				{
					return i;
				}
			}
		}
		return -1;
	}

	public static void Add<T>(this List<T> list, params T[] items)
	{
		for (int i = 0; i < items.Length; i++)
		{
			list.Add(items[i]);
		}
	}
}
