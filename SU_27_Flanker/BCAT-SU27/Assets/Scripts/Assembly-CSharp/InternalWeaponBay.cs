using System;
using System.Collections.Generic;
using UnityEngine;

public class InternalWeaponBay : MonoBehaviour
{
	public int hardpointIdx;

	public WeaponManager weaponManager;

	public RotationToggle rotationToggle;

	public AnimationToggle animationToggle;

	public bool externallyControlled;

	public bool openOnAnyWeaponMatch;

	public bool hideWhenClosed;

	private List<object> openReqs = new List<object>();

	public bool opening { get; private set; }

	public float doorState
	{
		get
		{
			if ((bool)rotationToggle)
			{
				return rotationToggle.transforms[0].currentT;
			}
			if ((bool)animationToggle)
			{
				return animationToggle.GetT();
			}
			return 0f;
		}
	}

	public float estTimeToOpen
	{
		get
		{
			float num = 0f;
			if ((bool)rotationToggle)
			{
				num = rotationToggle.transforms[0].speed / rotationToggle.transforms[0].angle;
			}
			else if ((bool)animationToggle)
			{
				num = animationToggle.time;
			}
			return num * (1f - doorState);
		}
	}

	public event Action<bool> OnStateChanged;

	public void RegisterOpenReq(object o)
	{
		if (!openReqs.Contains(o))
		{
			openReqs.Add(o);
		}
		Open();
	}

	public void UnregisterOpenReq(object o)
	{
		openReqs.Remove(o);
		if (openReqs.Count == 0)
		{
			Close();
		}
	}

	private void Awake()
	{
		weaponManager.OnWeaponEquipped += WeaponManager_OnWeaponEquipped;
	}

	private void Start()
	{
		weaponManager.OnWeaponChanged.AddListener(OnWeaponChanged);
		if (hideWhenClosed)
		{
			if ((bool)rotationToggle)
			{
				rotationToggle.OnFinishRetract += Hide;
				rotationToggle.OnStartDeploy += Show;
			}
			Hide();
		}
	}

	private void WeaponManager_OnWeaponEquipped(HPEquippable eq)
	{
		if (eq.hardpointIdx == hardpointIdx)
		{
			IUsesInternalWeaponBay[] componentsInChildrenImplementing = eq.gameObject.GetComponentsInChildrenImplementing<IUsesInternalWeaponBay>(includeInactive: true);
			for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
			{
				componentsInChildrenImplementing[i].SetInternalWeaponBay(this);
			}
			eq.rcsMasked = !opening;
		}
	}

	private void Hide()
	{
		weaponManager.hardpointTransforms[hardpointIdx].gameObject.SetActive(value: false);
	}

	private void Show()
	{
		weaponManager.hardpointTransforms[hardpointIdx].gameObject.SetActive(value: true);
	}

	private void OnWeaponChanged()
	{
		if (externallyControlled)
		{
			return;
		}
		bool flag = false;
		if (weaponManager.isMasterArmed && weaponManager.currentEquip != null)
		{
			if (weaponManager.currentEquip.hardpointIdx == hardpointIdx)
			{
				flag = true;
			}
			else if (openOnAnyWeaponMatch)
			{
				HPEquippable equip = weaponManager.GetEquip(hardpointIdx);
				if ((bool)equip && weaponManager.currentEquip.shortName == equip.shortName)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			Open();
		}
		else
		{
			Close();
		}
	}

	private void Open()
	{
		if (!opening)
		{
			opening = true;
			weaponManager.SetRCSDirty();
			this.OnStateChanged?.Invoke(opening);
		}
		if ((bool)rotationToggle)
		{
			rotationToggle.SetDeployed();
		}
		if ((bool)animationToggle)
		{
			animationToggle.Deploy();
		}
		HPEquippable equip = weaponManager.GetEquip(hardpointIdx);
		if ((bool)equip)
		{
			equip.rcsMasked = false;
		}
	}

	private void Close()
	{
		if (opening)
		{
			opening = false;
			weaponManager.SetRCSDirty();
			this.OnStateChanged?.Invoke(opening);
		}
		if ((bool)rotationToggle)
		{
			rotationToggle.SetDefault();
		}
		if ((bool)animationToggle)
		{
			animationToggle.Retract();
		}
		HPEquippable equip = weaponManager.GetEquip(hardpointIdx);
		if ((bool)equip)
		{
			equip.rcsMasked = true;
		}
	}
}
