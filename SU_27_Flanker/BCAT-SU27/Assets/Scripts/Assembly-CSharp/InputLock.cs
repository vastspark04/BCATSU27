using System.Collections.Generic;
using UnityEngine;

public class InputLock
{
	private List<string> locks = new List<string>();

	public string name;

	public bool isLocked => locks.Count > 0;

	public InputLock(string name)
	{
		this.name = name;
	}

	public void AddLock(string id)
	{
		if (locks.Contains(id))
		{
			Debug.LogWarning("Tried to add more than one " + name + " lock for ID: " + id);
		}
		else
		{
			locks.Add(id);
		}
	}

	public void RemoveLock(string id)
	{
		locks.Remove(id);
	}
}
