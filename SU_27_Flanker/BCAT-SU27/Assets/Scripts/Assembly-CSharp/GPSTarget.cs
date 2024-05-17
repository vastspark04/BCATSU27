using System;
using System.Text;
using UnityEngine;

public class GPSTarget
{
	private FixedPoint fixedPoint;

	public string denom;

	public int numeral;

	public Vector3 worldPosition => fixedPoint.point;

	public string targetName => $"{denom} {numeral}";

	public string fullGpsLabel => targetName + " " + GPSCoordsToString(PositionToGPSCoords(worldPosition));

	public GPSTarget(Vector3 worldPosition, string denom, int numeral)
	{
		fixedPoint = new FixedPoint(worldPosition);
		this.denom = denom.ToUpper();
		this.numeral = numeral;
	}

	public static Vector3 PositionToGPSCoords(Vector3 worldPosition)
	{
		float y = worldPosition.y - WaterPhysics.instance.height;
		Vector3 toVector = FloatingOrigin.accumOffset.toVector3;
		float num = (worldPosition.z + toVector.z) / 111319.9f;
		float num2 = worldPosition.x + toVector.x;
		float num3 = Mathf.Cos(num * ((float)Math.PI / 180f)) * 111319.9f;
		float num4 = 0f;
		if (num3 > 0f)
		{
			num4 = num2 / num3;
		}
		float num5 = num4;
		if ((bool)VTMapManager.fetch && (bool)VTMapManager.fetch.map)
		{
			num += VTMapManager.fetch.map.mapLatitude;
			num5 += VTMapManager.fetch.map.mapLongitude;
		}
		else if ((bool)LevelBuilder.fetch)
		{
			num += LevelBuilder.fetch.uiMap.mapLatitude;
			num5 += LevelBuilder.fetch.uiMap.mapLongitude;
		}
		return new Vector3(num5, y, num);
	}

	public static string GPSCoordsToString(Vector3 gpsCoords)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = Mathf.FloorToInt(gpsCoords.z);
		int value = Mathf.FloorToInt((gpsCoords.z - (float)num) * 60f);
		int num2 = Mathf.FloorToInt(gpsCoords.x);
		int value2 = Mathf.FloorToInt((gpsCoords.x - (float)num2) * 60f);
		if (num > 0)
		{
			stringBuilder.Append('+');
		}
		stringBuilder.Append(num);
		stringBuilder.Append("°");
		stringBuilder.Append(value);
		stringBuilder.Append('N');
		if (num2 > 0)
		{
			stringBuilder.Append('+');
		}
		stringBuilder.Append(num2);
		stringBuilder.Append("°");
		stringBuilder.Append(value2);
		stringBuilder.Append('E');
		return stringBuilder.ToString();
	}
}
