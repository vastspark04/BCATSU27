using UnityEngine;

public class ABObjectToggler : MonoBehaviour, IPersistentVehicleData
{
	public GameObject[] aObjects;

	public GameObject[] bObjects;

	public bool aDefault = true;

	public bool isPersistent = true;

	private bool isA = true;

	private void Awake()
	{
		aObjects.SetActive(aDefault);
		bObjects.SetActive(!aDefault);
		isA = aDefault;
	}

	public void Toggle()
	{
		isA = !isA;
		aObjects.SetActive(isA);
		bObjects.SetActive(!isA);
	}

	public void SetToA()
	{
		isA = true;
		aObjects.SetActive(active: true);
		bObjects.SetActive(active: false);
		aDefault = true;
	}

	public void SetToB()
	{
		isA = false;
		aObjects.SetActive(active: false);
		bObjects.SetActive(active: true);
		aDefault = false;
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		if (!base.gameObject)
		{
			Debug.LogError("Tried to quicksave ABObjectToggler but gameObject was null!");
		}
		else if (isPersistent)
		{
			vDataNode.SetValue(DataValueName(), isA);
		}
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (!isPersistent)
		{
			return;
		}
		string text = DataValueName();
		if (vDataNode.HasValue(text))
		{
			isA = ConfigNodeUtils.ParseBool(vDataNode.GetValue(text));
			aDefault = isA;
			if (isA)
			{
				SetToA();
			}
			else
			{
				SetToB();
			}
		}
	}

	private string DataValueName()
	{
		return base.gameObject.name + "Toggler";
	}
}
