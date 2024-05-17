using UnityEngine;

public class VRSwitchCover : MonoBehaviour
{
	public VRInteractable coveredSwitch;

	public bool setOffOnClosed;

	public bool covered { get; private set; }

	public void OnSetState(int st)
	{
		coveredSwitch.enabled = st == 0;
		covered = !coveredSwitch.enabled;
		if (covered && setOffOnClosed)
		{
			coveredSwitch.GetComponent<VRLever>().RemoteSetState(0);
		}
	}
}
