using UnityEngine;

public class TestStreetPosSample : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		Gizmos.color = (VTMapManager.IsPositionOverCityStreet(base.transform.position) ? Color.green : Color.red);
		Gizmos.DrawSphere(base.transform.position, 2f);
		float num = 153.6f;
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(base.transform.position);
		int num2 = Mathf.FloorToInt((float)vector3D.x / num);
		int num3 = Mathf.FloorToInt((float)vector3D.z / num);
		BDTexture hmBdt = VTMapGenerator.fetch.hmBdt;
		if (num2 >= 0 && num2 < hmBdt.width && num3 >= 0 && num3 < hmBdt.height)
		{
			Gizmos.color = VTMapGenerator.fetch.hmBdt.GetPixel(num2, num3).ToColor();
			Gizmos.DrawSphere(base.transform.position + new Vector3(0f, 3f, 0f), 1f);
		}
		Gizmos.color = Color.white;
	}
}
