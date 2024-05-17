using UnityEngine;

public struct FixedPoint : IConfigValue
{
	public Vector3D globalPoint;

	public Vector3 point
	{
		get
		{
			return VTMapManager.GlobalToWorldPoint(globalPoint);
		}
		set
		{
			globalPoint = VTMapManager.WorldToGlobalPoint(value);
		}
	}

	public FixedPoint(Vector3 worldPosition)
	{
		globalPoint = VTMapManager.WorldToGlobalPoint(worldPosition);
	}

	public FixedPoint(Vector3D globalPosition)
	{
		globalPoint = globalPosition;
	}

	public string WriteValue()
	{
		return ConfigNodeUtils.WriteVector3D(globalPoint);
	}

	public void ConstructFromValue(string s)
	{
		globalPoint = ConfigNodeUtils.ParseVector3D(s);
	}
}
