using UnityEngine;

public class MultiObjectSwitcher : MonoBehaviour, IQSVehicleComponent, IPersistentVehicleData
{
	public GameObject[] objects;

	public int defaultObject;

	public bool qsPersistent = true;

	public bool vDataPersistent = true;

	public int currIdx { get; private set; }

	private string nodeName => "MultiObjectSwitcher_" + base.gameObject.name;

	private void Start()
	{
		SetObject(defaultObject);
	}

	public void SetObject(int o)
	{
		currIdx = o;
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].SetActive(i == o);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode(nodeName).SetValue("currIdx", currIdx);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (qsNode.GetNode(nodeName).TryGetValue<int>("currIdx", out var value))
		{
			defaultObject = value;
			SetObject(value);
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		vDataNode.AddNode(nodeName).SetValue("currIdx", currIdx);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNode node = vDataNode.GetNode(nodeName);
		if (node != null && node.TryGetValue<int>("currIdx", out var value))
		{
			defaultObject = value;
			SetObject(value);
		}
	}
}
