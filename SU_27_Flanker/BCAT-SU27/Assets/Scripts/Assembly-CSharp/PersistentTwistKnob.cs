using UnityEngine;

public class PersistentTwistKnob : MonoBehaviour, IPersistentVehicleData
{
	public VRTwistKnob knob;

	public string dataName;

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		vDataNode.SetValue(dataName, ConfigNodeUtils.WriteObject(knob.currentValue));
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (vDataNode.HasValue(dataName))
		{
			float num = ConfigNodeUtils.ParseFloat(vDataNode.GetValue(dataName));
			knob.startValue = num;
			knob.SetKnobValue(num);
		}
	}
}
