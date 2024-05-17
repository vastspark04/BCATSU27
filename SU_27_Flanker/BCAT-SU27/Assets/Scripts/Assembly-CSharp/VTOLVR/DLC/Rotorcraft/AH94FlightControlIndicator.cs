using UnityEngine;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94FlightControlIndicator : MonoBehaviour
{
	public MultiUserVehicleSync muvs;

	public Color controlColor;

	public Color requestingColor;

	public UIImageStatusLight localStatus;

	public UIImageStatusLight remoteStatus;

	private int localState = -1;

	private int remoteState = -1;

	private Color[] colors;

	private void Awake()
	{
		colors = new Color[3];
		colors[0] = Color.black;
		colors[1] = requestingColor;
		colors[2] = controlColor;
	}

	private void Start()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			localStatus.SetColor(controlColor);
			remoteStatus.SetColor(Color.black);
			base.enabled = false;
		}
	}

	private void Update()
	{
		int num = 0;
		int num2 = 0;
		if (muvs.IsControlOwner())
		{
			num = 2;
			if (muvs.isRemoteControlRequesting)
			{
				num2 = 1;
			}
		}
		else
		{
			if (muvs.isLocalControlRequesting)
			{
				num = 1;
			}
			num2 = 2;
		}
		if (localState != num || remoteState != num2)
		{
			localState = num;
			remoteState = num2;
			localStatus.SetColor(colors[localState]);
			remoteStatus.SetColor(colors[remoteState]);
		}
	}
}

}