using UnityEngine;
using UnityEngine.UI;

public class HUDWeaponInfo : MonoBehaviour, ILocalizationUser
{
	private HPEquippable weapon;

	public Text weaponNameText;

	public Text ammoCountText;

	public Text subLabelText;

	public GameObject fireIndicator;

	public HUDGunDirectorSight gds;

	public HUDCCIPSight ccip;

	public HUDGPSBombTargeter gpsBombTargeter;

	public GameObject opticalLoalDirectionIndicator;

	public Transform leadIndicator;

	[HideInInspector]
	public WeaponManager wm;

	public RectTransform[] reticleTransforms;

	private int reticleIndex;

	private RectTransform reticleTransform;

	private float hudDepth;

	public IWeaponLeadIndicator leadIndicatorEquip;

	private string s_noArm;

	private bool isOpticalLoalMissile;

	private bool remoteAiming;

	public void ApplyLocalization()
	{
		s_noArm = VTLocalizationManager.GetString("hud_noArm", "NO ARM", "A short HUD label when no weapons are armed.");
	}

	private void Awake()
	{
		ApplyLocalization();
		fireIndicator.SetActive(value: false);
		RectTransform[] array = reticleTransforms;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
		if ((bool)gds)
		{
			gds.gameObject.SetActive(value: false);
		}
		if ((bool)ccip)
		{
			ccip.gameObject.SetActive(value: false);
		}
		hudDepth = GetComponentInParent<CollimatedHUDUI>().depth;
		if ((bool)leadIndicator)
		{
			leadIndicator.gameObject.SetActive(value: false);
		}
		Debug.Log("HUDWeaponInfo Awake");
		SetWeapon(weapon);
	}

	private void SetLeadIndicatorPosition(Vector3 worldPosition)
	{
		if ((bool)leadIndicator)
		{
			Vector3 normalized = (worldPosition - VRHead.position).normalized;
			normalized = Vector3.Slerp(normalized, wm.transform.forward, 0.5f);
			leadIndicator.position = VRHead.position + normalized * hudDepth;
			leadIndicator.rotation = Quaternion.LookRotation(normalized, leadIndicator.parent.up);
		}
	}

	public void SetWeapon(HPEquippable weapon)
	{
		isOpticalLoalMissile = false;
		this.weapon = weapon;
		if ((bool)gds)
		{
			gds.gameObject.SetActive(value: false);
		}
		if ((bool)ccip)
		{
			ccip.gameObject.SetActive(value: false);
		}
		if ((bool)gpsBombTargeter)
		{
			gpsBombTargeter.gameObject.SetActive(value: false);
		}
		if ((bool)opticalLoalDirectionIndicator)
		{
			opticalLoalDirectionIndicator.SetActive(value: false);
		}
		if ((bool)weapon)
		{
			weaponNameText.text = weapon.shortName.ToUpper();
			RectTransform[] array = reticleTransforms;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
			reticleTransform = null;
			if ((bool)gds && weapon is IGDSCompatible)
			{
				IGDSCompatible iGDSCompatible = (IGDSCompatible)weapon;
				gds.fireTransform = iGDSCompatible.GetFireTransform();
				gds.bulletVelocity = iGDSCompatible.GetMuzzleVelocity();
				gds.weapon = weapon;
				gds.gameObject.SetActive(value: true);
			}
			else if (weapon.GetCount() > 0 && (bool)ccip && weapon is ICCIPCompatible)
			{
				if ((bool)gpsBombTargeter && weapon is IGuidedBombWeapon && !((IGuidedBombWeapon)weapon).IsDumbMode())
				{
					gpsBombTargeter.guidedBombRack = (IGuidedBombWeapon)weapon;
					gpsBombTargeter.gameObject.SetActive(value: true);
				}
				else
				{
					ICCIPCompatible iCCIPCompatible = (ICCIPCompatible)weapon;
					ccip.SetWeapon(iCCIPCompatible);
					ccip.gameObject.SetActive(value: true);
				}
			}
			else if (weapon.GetCount() > 0)
			{
				reticleIndex = weapon.GetReticleIndex();
				reticleTransform = reticleTransforms[reticleIndex];
				reticleTransform.gameObject.SetActive(value: true);
			}
			if ((bool)leadIndicator)
			{
				if (weapon is IWeaponLeadIndicator)
				{
					leadIndicatorEquip = (IWeaponLeadIndicator)weapon;
				}
				else
				{
					leadIndicatorEquip = null;
					leadIndicator.gameObject.SetActive(value: false);
				}
			}
			ammoCountText.gameObject.SetActive(value: true);
			subLabelText.text = weapon.GetLocalizedSublabel();
			if (weapon is HPEquipOpticalML && ((HPEquipOpticalML)weapon).ml.missilePrefab.GetComponent<Missile>().opticalLOAL)
			{
				isOpticalLoalMissile = true;
			}
		}
		else
		{
			weaponNameText.text = s_noArm;
			if ((bool)reticleTransform)
			{
				reticleTransform.gameObject.SetActive(value: false);
			}
			ammoCountText.gameObject.SetActive(value: false);
			subLabelText.text = string.Empty;
			if ((bool)leadIndicator)
			{
				leadIndicator.gameObject.SetActive(value: false);
			}
		}
	}

	public void RefreshWeaponInfo()
	{
		SetWeapon(weapon);
	}

	public void SetRemoteAimPoint(Vector3 worldPoint)
	{
		remoteAiming = true;
		if ((bool)reticleTransform)
		{
			reticleTransform.gameObject.SetActive(value: true);
			Vector3 normalized = (worldPoint - VRHead.position).normalized;
			reticleTransform.position = VRHead.position + normalized * hudDepth;
			reticleTransform.rotation = Quaternion.LookRotation(normalized, reticleTransform.parent.up);
		}
	}

	public void SetLocalAim()
	{
		remoteAiming = false;
	}

	private void LateUpdate()
	{
		if (weapon != null && weapon.itemActivated)
		{
			ammoCountText.text = wm.combinedCount.ToString();
			if (!remoteAiming && (bool)reticleTransform)
			{
				reticleTransform.gameObject.SetActive(value: true);
				Vector3 normalized = (weapon.GetAimPoint() - VRHead.position).normalized;
				reticleTransform.position = VRHead.position + normalized * hudDepth;
				reticleTransform.rotation = Quaternion.LookRotation(normalized, reticleTransform.parent.up);
			}
			if (leadIndicatorEquip != null)
			{
				if (leadIndicatorEquip.GetShowLeadIndicator())
				{
					leadIndicator.gameObject.SetActive(value: true);
					SetLeadIndicatorPosition(leadIndicatorEquip.GetLeadIndicatorPosition());
				}
				else
				{
					leadIndicator.gameObject.SetActive(value: false);
				}
			}
			if (isOpticalLoalMissile && (bool)opticalLoalDirectionIndicator)
			{
				if (weapon.LaunchAuthorized() && (!wm.opticalTargeter || wm.opticalTargeter.laserOccluded))
				{
					opticalLoalDirectionIndicator.SetActive(value: true);
				}
				else
				{
					opticalLoalDirectionIndicator.SetActive(value: false);
				}
			}
		}
		else if ((bool)reticleTransform)
		{
			reticleTransform.gameObject.SetActive(value: false);
		}
	}

	public void StartFire()
	{
		fireIndicator.SetActive(value: true);
	}

	public void StopFire()
	{
		fireIndicator.SetActive(value: false);
	}
}
