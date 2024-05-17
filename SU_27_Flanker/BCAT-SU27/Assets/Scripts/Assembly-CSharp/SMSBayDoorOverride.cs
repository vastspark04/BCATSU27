using UnityEngine;
using UnityEngine.UI;

public class SMSBayDoorOverride : MonoBehaviour, IQSVehicleComponent, ILocalizationUser
{
	public InternalWeaponBay[] weaponBays;

	public Text statusText;

	private string s_bdo_open;

	private string s_bdo_auto;

	private bool overrideOpen;

	private string nodeName => base.gameObject.name + "_SMSBayDoorOverride";

	public void ApplyLocalization()
	{
		s_bdo_open = VTLocalizationManager.GetString("s_bdo_open", "OPEN", "SMS bay door override in OPEN position");
		s_bdo_auto = VTLocalizationManager.GetString("s_bdo_auto", "AUTO", "SMS bay door override in AUTO position");
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	public void OnPressToggle()
	{
		overrideOpen = !overrideOpen;
		UpdateBay();
	}

	private void Start()
	{
		UpdateBay();
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode(nodeName).SetValue("overrideOpen", overrideOpen);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(nodeName);
		if (node != null)
		{
			overrideOpen = node.GetValue<bool>("overrideOpen");
			UpdateBay();
		}
	}

	public void SetOverride(bool _override)
	{
		overrideOpen = _override;
		UpdateBay();
	}

	private void UpdateBay()
	{
		if (overrideOpen)
		{
			InternalWeaponBay[] array = weaponBays;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].RegisterOpenReq(this);
			}
		}
		else
		{
			InternalWeaponBay[] array = weaponBays;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].UnregisterOpenReq(this);
			}
		}
		statusText.text = (overrideOpen ? s_bdo_open : s_bdo_auto);
	}
}
