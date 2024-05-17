using UnityEngine;

public class MarkJettisonIndividual : MonoBehaviour
{
	public WeaponManager wm;

	public UIImageToggle indicator;

	public int storeIdx;

	private void Awake()
	{
		wm.OnWeaponChanged.AddListener(OnWeaponChanged);
	}

	private void Start()
	{
		OnWeaponChanged();
	}

	private void OnWeaponChanged()
	{
		HPEquippable equip = wm.GetEquip(storeIdx);
		indicator.imageEnabled = (bool)equip && equip.markedForJettison;
	}

	public void PressButton()
	{
		HPEquippable equip = wm.GetEquip(storeIdx);
		if ((bool)equip && equip.jettisonable)
		{
			equip.markedForJettison = !equip.markedForJettison;
			wm.ReportEquipJettisonMark(equip);
			wm.RefreshWeapon();
		}
	}
}
