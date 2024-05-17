using UnityEngine;

public class MFDPortalHalf : MonoBehaviour
{
	public MFDPortalManager manager;

	public MFDPortalQuarter quarterLeft;

	public MFDPortalQuarter quarterRight;

	public MFDPortalPageSelectButton hiddenMinButton;

	private void Awake()
	{
		hiddenMinButton.gameObject.SetActive(value: false);
	}

	public void HiddenMinButton()
	{
		if (quarterLeft.displayState == MFDPortalQuarter.DisplayStates.Maximized)
		{
			quarterLeft.HideSubs();
			quarterRight.MaximizeDisplay();
		}
		else
		{
			quarterRight.HideSubs();
			quarterLeft.MaximizeDisplay();
		}
	}

	public void PlayInputSound()
	{
		manager.PlayInputSound();
	}
}
