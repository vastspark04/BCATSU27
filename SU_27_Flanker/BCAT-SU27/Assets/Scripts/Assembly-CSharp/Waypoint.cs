using UnityEngine;

public class Waypoint
{
	public int id;

	public Vector3D globalPoint;

	private string _name;

	private Transform transform;

	public string name
	{
		get
		{
			return GetName();
		}
		set
		{
			SetName(value);
		}
	}

	public Vector3 worldPosition => GetTransform().position;

	public virtual string GetName()
	{
		return _name;
	}

	public virtual void SetName(string n)
	{
		_name = n;
	}

	public virtual void SetTransform(Transform tf)
	{
		transform = tf;
	}

	public virtual Transform GetTransform()
	{
		return transform;
	}
}
