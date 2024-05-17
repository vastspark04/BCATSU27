using UnityEngine;
using UnityEngine.UI;

public class MFDJoystickOptions : MonoBehaviour, IPersistentVehicleData
{
	public Text stickSensitivityText;

	private int stickSensIdx = 2;

	private float[] sensOptions = new float[7] { 0.6f, 0.8f, 1f, 1.2f, 1.4f, 1.6f, 1.8f };

	private VRJoystick[] sticks;

	private void Start()
	{
		UpdateStickSensitivity();
	}

	public void ToggleStickSensitivity()
	{
		stickSensIdx = (stickSensIdx + 1) % sensOptions.Length;
		UpdateStickSensitivity();
	}

	private void UpdateStickSensitivity()
	{
		if (sticks == null)
		{
			sticks = base.transform.root.GetComponentsInChildren<VRJoystick>(includeInactive: true);
		}
		if ((bool)stickSensitivityText)
		{
			stickSensitivityText.text = sensOptions[stickSensIdx].ToString("0.00");
		}
		VRJoystick[] array = sticks;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sensitivity = sensOptions[stickSensIdx];
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		vDataNode.AddOrGetNode("MFDVehicleOptions").SetValue("stickSensIdx", stickSensIdx);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNode node = vDataNode.GetNode("MFDVehicleOptions");
		if (node != null)
		{
			stickSensIdx = node.GetValue<int>("stickSensIdx");
		}
		UpdateStickSensitivity();
	}
}
