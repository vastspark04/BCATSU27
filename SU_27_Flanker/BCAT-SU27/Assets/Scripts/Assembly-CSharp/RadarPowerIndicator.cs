using UnityEngine;
using UnityEngine.UI;

public class RadarPowerIndicator : MonoBehaviour
{
	public Image image;

	public Radar radar;

	public Color onColor;

	public Color offColor;

	private void Start()
	{
		radar.OnRadarEnabled += UpdateImage;
		UpdateImage(radar.radarEnabled);
	}

	private void UpdateImage(bool on)
	{
		image.color = (on ? onColor : offColor);
	}
}
