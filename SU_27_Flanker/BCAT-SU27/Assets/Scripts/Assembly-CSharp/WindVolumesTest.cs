using UnityEngine;

public class WindVolumesTest : MonoBehaviour
{
	public float maxHPos = 1000f;

	public float maxVPos = 1000f;

	public float interval = 10f;

	public float lengthMult = 100f;

	private void Update()
	{
		float maxHorizontalGustspeed = WindVolumes.instance.maxHorizontalGustspeed;
		for (float num = 0f - maxHPos; num < maxHPos; num += interval)
		{
			for (float num2 = 0f; num2 < maxVPos; num2 += interval)
			{
				for (float num3 = 0f - maxHPos; num3 < maxHPos; num3 += interval)
				{
					Vector3 vector = new Vector3(num, num2, num3);
					Vector3 wind = WindVolumes.instance.GetWind(vector);
					Debug.DrawLine(color: new Color(Mathf.Abs(wind.x) / maxHorizontalGustspeed, Mathf.Abs(wind.z) / maxHorizontalGustspeed, vector.y / maxVPos), start: vector, end: vector + wind * lengthMult);
				}
			}
		}
	}
}
