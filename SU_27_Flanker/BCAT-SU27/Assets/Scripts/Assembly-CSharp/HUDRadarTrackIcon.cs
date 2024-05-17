using UnityEngine;
using UnityEngine.UI;

public class HUDRadarTrackIcon : MonoBehaviour
{
	public Image trackCircle;

	public Text trackText;

	private float depth;

	private void Start()
	{
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
	}

	public void SetTrack(Vector3 tgtPosition, bool showCircle)
	{
		Vector3 normalized = (tgtPosition - VRHead.position).normalized;
		base.transform.position = VRHead.position + normalized * depth;
		base.transform.rotation = Quaternion.LookRotation(normalized, base.transform.parent.up);
		trackCircle.gameObject.SetActive(showCircle);
	}
}
