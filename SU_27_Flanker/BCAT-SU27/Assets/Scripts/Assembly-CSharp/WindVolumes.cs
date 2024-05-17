using UnityEngine;

public class WindVolumes : MonoBehaviour
{
	private class WindVoxel
	{
		private Vector3 tOffset;

		private Vector3 randTimeScale;

		public Vector3 GetWind(float timeScale, float maxH, float maxV)
		{
			float num = Time.time + randomTimeOffset;
			float x = maxH * VectorUtils.Triangle(randTimeScale.x * timeScale * (num + tOffset.x));
			float y = maxV * VectorUtils.Triangle(randTimeScale.y * timeScale * (num + tOffset.y));
			float z = maxH * VectorUtils.Triangle(randTimeScale.z * timeScale * (num + tOffset.z));
			return new Vector3(x, y, z);
		}

		public WindVoxel()
		{
			tOffset = new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));
			randTimeScale = new Vector3(Random.Range(0.1f, 1f), Random.Range(0.1f, 1f), Random.Range(0.1f, 1f));
		}
	}

	public static float randomTimeOffset;

	public static bool windEnabled = true;

	public bool windEnabledInScene = true;

	public Vector3 sustainedWind;

	public float windHVariation;

	public float windVVariation;

	public float windVariationTimescale;

	public float horizontalSize = 20000f;

	public float verticalSize = 20000f;

	public int horizontalArraySize = 8;

	public int verticalArraySize = 3;

	public float maxHorizontalGustspeed = 40f;

	public float maxVerticalGustspeed = 5f;

	public float gustTimescale = 1f;

	private WindVoxel windVoxel;

	private WindVoxel[][][] gustArray;

	public static WindVolumes instance { get; private set; }

	private void Awake()
	{
		GenerateArray();
		instance = this;
		windVoxel = new WindVoxel();
		randomTimeOffset = Random.Range(0f, 1600f);
		windEnabled = GameSettings.CurrentSettings.GetBoolSetting("EXPERIMENTAL_WIND");
	}

	private void GenerateArray()
	{
		gustArray = new WindVoxel[horizontalArraySize][][];
		for (int i = 0; i < horizontalArraySize; i++)
		{
			gustArray[i] = new WindVoxel[verticalArraySize][];
			for (int j = 0; j < verticalArraySize; j++)
			{
				gustArray[i][j] = new WindVoxel[horizontalArraySize];
				for (int k = 0; k < horizontalArraySize; k++)
				{
					gustArray[i][j][k] = new WindVoxel();
				}
			}
		}
	}

	public Vector3 GetWind(Vector3 worldPosition)
	{
		if (!windEnabledInScene)
		{
			return Vector3.zero;
		}
		Vector3 vector = worldPosition - FloatingOrigin.accumOffset.toVector3;
		vector.x += horizontalSize * (float)horizontalArraySize / 2f;
		vector.z += horizontalSize * (float)horizontalArraySize / 2f;
		vector.y = WaterPhysics.GetAltitude(worldPosition);
		float num = vector.x / horizontalSize;
		int value = Mathf.FloorToInt(num);
		int value2 = Mathf.CeilToInt(num);
		value = Mathf.Clamp(value, 0, horizontalArraySize - 1);
		value2 = Mathf.Clamp(value2, 0, horizontalArraySize - 1);
		float num2 = vector.z / horizontalSize;
		int value3 = Mathf.FloorToInt(num2);
		int value4 = Mathf.CeilToInt(num2);
		value3 = Mathf.Clamp(value3, 0, horizontalArraySize - 1);
		value4 = Mathf.Clamp(value4, 0, horizontalArraySize - 1);
		float num3 = vector.y / verticalSize;
		int value5 = Mathf.FloorToInt(num3);
		int value6 = Mathf.CeilToInt(num3);
		value5 = Mathf.Clamp(value5, 0, verticalArraySize - 1);
		value6 = Mathf.Clamp(value6, 0, verticalArraySize - 1);
		float t = num - (float)value;
		float t2 = num2 - (float)value3;
		Vector3 wind = gustArray[value][value5][value3].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind2 = gustArray[value2][value5][value3].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind3 = gustArray[value][value5][value4].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind4 = gustArray[value2][value5][value4].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind5 = gustArray[value][value6][value3].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind6 = gustArray[value2][value6][value3].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind7 = gustArray[value][value6][value4].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 wind8 = gustArray[value2][value6][value4].GetWind(gustTimescale, maxHorizontalGustspeed, maxVerticalGustspeed);
		Vector3 a = Vector3.Lerp(wind, wind2, t);
		Vector3 b = Vector3.Lerp(wind3, wind4, t);
		Vector3 a2 = Vector3.Lerp(a, b, t2);
		Vector3 a3 = Vector3.Lerp(wind5, wind6, t);
		Vector3 b2 = Vector3.Lerp(wind7, wind8, t);
		Vector3 b3 = Vector3.Lerp(a3, b2, t2);
		float t3 = num3 - (float)value5;
		Vector3 vector2 = Vector3.Lerp(a2, b3, t3);
		Vector3 vector3 = sustainedWind + windVoxel.GetWind(windVariationTimescale, windHVariation, windVVariation);
		return vector2 + vector3;
	}
}
