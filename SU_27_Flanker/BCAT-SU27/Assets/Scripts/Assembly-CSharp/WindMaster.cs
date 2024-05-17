using UnityEngine;

public class WindMaster : MonoBehaviour
{
	public Vector3 wind;

	private void Update()
	{
		if (WindVolumes.windEnabled)
		{
			wind = WindVolumes.instance.GetWind(base.transform.position);
		}
		else
		{
			base.enabled = false;
		}
	}
}
