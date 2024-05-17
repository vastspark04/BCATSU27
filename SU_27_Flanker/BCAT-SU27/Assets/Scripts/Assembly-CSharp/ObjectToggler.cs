using UnityEngine;

public class ObjectToggler : MonoBehaviour, IQSVehicleComponent
{
	public bool qsVehiclePersistent;

	public void ToggleObject()
	{
		base.gameObject.SetActive(!base.gameObject.activeSelf);
	}

	public void SetObjectActive(int a)
	{
		base.gameObject.SetActive(a > 0);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (qsVehiclePersistent)
		{
			string nodeName = base.gameObject.name + "_ObjectToggler";
			qsNode.AddNode(nodeName).SetValue("active", base.gameObject.activeSelf);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (qsVehiclePersistent)
		{
			string text = base.gameObject.name + "_ObjectToggler";
			ConfigNode node = qsNode.GetNode(text);
			if (node != null)
			{
				base.gameObject.SetActive(node.GetValue<bool>("active"));
			}
		}
	}
}
