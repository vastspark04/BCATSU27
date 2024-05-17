using UnityEngine;

public class BlackoutEffectCamera : MonoBehaviour
{
	public float dist;

	public float scale;

	public int layer = 28;

	private void OnPreCull()
	{
		BlackoutEffect.instance.SetForCamera(base.transform, dist, scale, layer);
	}
}
