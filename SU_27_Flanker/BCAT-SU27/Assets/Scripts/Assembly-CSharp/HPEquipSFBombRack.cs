using UnityEngine;

public class HPEquipSFBombRack : HPEquipBombRack, ICCIPCompatible, IRippleWeapon, IRequiresOpticalTargeter, ILocalizationUser
{
	private float deployAlt;

	private float subEjectTime;

	private float subMass;

	private float subDragArea;

	private float chuteRate;

	private FlightInfo flightInfo;

	private string hudMsgId;

	private string s_cbuAlt;

	private bool hasSetAltMsg;

	private bool gotSubProps;

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_cbuAlt = VTLocalizationManager.GetString("s_cbuAlt", "BELOW CBU ALT", "HUD when below submunition deploy altitude of cluster bomb");
	}

	private void Update()
	{
		if (!base.weaponManager || !base.weaponManager.vm || !base.weaponManager.vm.hudMessages)
		{
			return;
		}
		if (base.itemActivated && flightInfo.radarAltitude < deployAlt)
		{
			if (!hasSetAltMsg)
			{
				base.weaponManager.vm.hudMessages.SetMessage(hudMsgId, s_cbuAlt);
				hasSetAltMsg = true;
			}
		}
		else if (hasSetAltMsg)
		{
			base.weaponManager.vm.hudMessages.RemoveMessage(hudMsgId);
			hasSetAltMsg = false;
		}
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		ml.OnLoadMissile -= Ml_OnLoadMissile;
		ml.OnLoadMissile += Ml_OnLoadMissile;
		TryGetSubmunitionProperties();
		flightInfo = base.weaponManager.GetComponent<FlightInfo>();
		hudMsgId = shortName + "sfInfo";
	}

	private void Ml_OnLoadMissile(Missile obj)
	{
		TryGetSubmunitionProperties();
	}

	private void TryGetSubmunitionProperties()
	{
		if (!gotSubProps && ml.missiles[0] != null)
		{
			SensorFuzedCB component = ml.missiles[0].GetComponent<SensorFuzedCB>();
			SFSubmunition sFSubmunition = component.subGroups[0].submunitions[0];
			subEjectTime = component.subGroups[0].delay;
			deployAlt = component.deployAltitude;
			if (component.overrideSubmunitions)
			{
				subMass = component.subMass;
				subDragArea = component.chuteDragArea;
				chuteRate = component.chuteDeployRate;
			}
			else
			{
				subMass = sFSubmunition.mass;
				subDragArea = sFSubmunition.chuteDrag.area;
				chuteRate = sFSubmunition.chuteDeployRate;
			}
			gotSubProps = true;
		}
	}

	public override Vector3 GetImpactPointWithLead(out float t)
	{
		bool flag = (bool)targeter && targeter.locked;
		float timeLimit = (flag ? 120 : 30);
		float simDeltaTime = (flag ? 0.1f : 0.2f);
		Vector3 targeterPosition = (flag ? targeter.lockTransform.position : Vector3.zero);
		_impactPoint = GetCBUBombImpactPoint(out t, ml, simDeltaTime, timeLimit, dragArea, bombMass, flag, targeterPosition);
		_leadTime = t;
		return _impactPoint;
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		HUDMessages hudMessages = base.weaponManager.vm.hudMessages;
		if ((bool)hudMessages && hasSetAltMsg)
		{
			hudMessages.RemoveMessage(hudMsgId);
			hasSetAltMsg = false;
		}
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		HUDMessages hudMessages = base.weaponManager.vm.hudMessages;
		if ((bool)hudMessages && hasSetAltMsg)
		{
			hudMessages.RemoveMessage(hudMsgId);
			hasSetAltMsg = false;
		}
	}

	private Vector3 GetCBUBombImpactPoint(out float time, MissileLauncher ml, float simDeltaTime, float timeLimit, float dragArea, float bombMass, bool hasTargeter, Vector3 targeterPosition)
	{
		time = -1f;
		if (!ml.parentRb)
		{
			return Vector3.zero;
		}
		float num = 0f;
		Vector3 vector = ml.transform.position;
		Vector3 velocity = ml.parentRb.velocity;
		float num2 = -0.5f * dragArea * simDeltaTime / bombMass;
		bool flag = false;
		float num3 = 0f;
		Ray ray = default(Ray);
		float num4 = 0f;
		if (!hasTargeter)
		{
			num4 = flightInfo.transform.position.y - flightInfo.radarAltitude;
		}
		for (; num < timeLimit; num += simDeltaTime)
		{
			Vector3 vector2 = vector;
			if (!flag)
			{
				float num5 = 0f;
				num5 = ((!hasTargeter) ? (vector2.y - num4) : (vector2.y - targeterPosition.y));
				if (num5 < deployAlt && num > 0.5f)
				{
					flag = true;
					num3 = num;
				}
			}
			if (flag && num - num3 > subEjectTime)
			{
				float num6 = Mathf.Clamp01((num - num3 - subEjectTime) * chuteRate);
				num2 = -0.5f * num6 * subDragArea * simDeltaTime / subMass;
			}
			velocity += Physics.gravity * simDeltaTime;
			velocity += num2 * AerodynamicsController.fetch.AtmosDensityAtPosition(vector2) * velocity.magnitude * velocity;
			vector2 += velocity * simDeltaTime;
			float enter;
			if (hasTargeter)
			{
				if (vector2.y < targeterPosition.y)
				{
					ray.origin = vector;
					ray.direction = velocity;
					if (new Plane(Vector3.up, targeterPosition).Raycast(ray, out enter))
					{
						vector = ray.GetPoint(enter);
						break;
					}
				}
			}
			else
			{
				if (vector2.y < WaterPhysics.instance.height)
				{
					ray.origin = vector;
					ray.direction = velocity;
					vector = ((!WaterPhysics.instance.waterPlane.Raycast(ray, out enter)) ? vector2 : ray.GetPoint(enter));
					break;
				}
				if (Physics.Linecast(vector, vector2, out var hitInfo, 1, QueryTriggerInteraction.Ignore))
				{
					vector = hitInfo.point;
					break;
				}
			}
			vector = vector2;
		}
		time = num;
		return vector;
	}
}
