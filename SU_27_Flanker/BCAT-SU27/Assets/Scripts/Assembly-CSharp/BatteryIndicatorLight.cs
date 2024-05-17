using UnityEngine;

public class BatteryIndicatorLight : MonoBehaviour
{
	public Battery battery;

	public EmissiveTextureLight emissiveLight;

	private void Update()
	{
		if (battery.connected && battery.currentCharge > 1f && battery.Drain(0.01f * Time.deltaTime))
		{
			Color color = Color.Lerp(Color.red, Color.green, battery.currentCharge / battery.maxCharge);
			emissiveLight.SetColor(color);
		}
		else
		{
			emissiveLight.SetColor(Color.black);
		}
	}
}
