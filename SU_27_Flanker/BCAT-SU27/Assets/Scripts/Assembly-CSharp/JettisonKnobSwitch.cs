using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JettisonKnobSwitch : MonoBehaviour
{
	private enum Modes
	{
		All,
		External,
		Selected
	}

	public WeaponManager weaponManager;

	public Transform buttonTf;

	public Vector3 buttonTfOffset;

	private Vector3 origButtonPos;

	public int[] extHardpoints;

	private Modes mode;

	private Coroutine buttonRoutine;

	private void Awake()
	{
		if ((bool)buttonTf)
		{
			origButtonPos = buttonTf.localPosition;
		}
	}

	public void SetMode(int m)
	{
		mode = (Modes)m;
	}

	public void JettisonButton()
	{
		switch (mode)
		{
		case Modes.All:
		{
			for (int k = 0; k < weaponManager.equipCount; k++)
			{
				HPEquippable equip3 = weaponManager.GetEquip(k);
				if (equip3 != null && equip3.jettisonable)
				{
					equip3.markedForJettison = true;
				}
				weaponManager.JettisonMarkedItems();
			}
			break;
		}
		case Modes.External:
		{
			List<int> list = new List<int>();
			for (int i = 0; i < weaponManager.equipCount; i++)
			{
				HPEquippable equip = weaponManager.GetEquip(i);
				if (!(equip != null))
				{
					continue;
				}
				if (equip.jettisonable && extHardpoints.Contains(i))
				{
					equip.markedForJettison = true;
					continue;
				}
				if (equip.markedForJettison)
				{
					list.Add(i);
				}
				equip.markedForJettison = false;
			}
			weaponManager.JettisonMarkedItems();
			for (int j = 0; j < list.Count; j++)
			{
				int idx = list[j];
				HPEquippable equip2 = weaponManager.GetEquip(idx);
				if ((bool)equip2 && equip2.jettisonable)
				{
					equip2.markedForJettison = true;
				}
			}
			weaponManager.RefreshWeapon();
			break;
		}
		case Modes.Selected:
			weaponManager.JettisonMarkedItems();
			break;
		}
		if ((bool)buttonTf)
		{
			if (buttonRoutine != null)
			{
				StopCoroutine(buttonRoutine);
			}
			buttonRoutine = StartCoroutine(ButtonRoutine());
		}
	}

	private IEnumerator ButtonRoutine()
	{
		Vector3 dPos = origButtonPos + buttonTfOffset;
		float t = 0f;
		while (t < 1f)
		{
			buttonTf.localPosition = Vector3.Lerp(dPos, origButtonPos, t);
			t += Time.deltaTime * 2f;
			yield return null;
		}
	}
}
