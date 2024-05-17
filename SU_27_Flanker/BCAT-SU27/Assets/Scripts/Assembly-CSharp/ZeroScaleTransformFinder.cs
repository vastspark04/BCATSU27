using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ZeroScaleTransformFinder : MonoBehaviour
{
	public bool execute;

	private void Start()
	{
	}

	private void Update()
	{
		if (execute)
		{
			execute = false;
			Execute();
		}
	}

	private void Execute()
	{
		Transform[] array = Object.FindObjectsOfType<Transform>();
		foreach (Transform transform in array)
		{
			if (transform.localScale == Vector3.zero)
			{
				LogTransform(transform);
			}
		}
	}

	private void LogTransform(Transform tf)
	{
		List<string> list = new List<string>();
		Transform transform = tf;
		while (true)
		{
			list.Insert(0, "/" + transform.name);
			if (!transform.parent)
			{
				break;
			}
			transform = transform.parent;
		}
		string text = string.Empty;
		foreach (string item in list)
		{
			text += item;
		}
		Debug.Log("Found zero scale tf: " + text);
	}
}
