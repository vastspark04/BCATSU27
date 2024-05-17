using UnityEngine;

public class HPEquipBombDragRack : HPEquipBombRack, ICCIPCompatible, IRippleWeapon, IRequiresOpticalTargeter
{
	private float dragDelay;

	private float initialDrag;

	private float deployedDrag;

	private bool gotChuteProps;

	protected override void OnEquip()
	{
		base.OnEquip();
		TryGetChuteProperties();
		ml.OnLoadMissile -= Ml_OnLoadMissile;
		ml.OnLoadMissile += Ml_OnLoadMissile;
	}

	private void Ml_OnLoadMissile(Missile obj)
	{
		TryGetChuteProperties();
	}

	private void TryGetChuteProperties()
	{
		if (!gotChuteProps && ml.missileCount > 0 && ml.missiles != null && ml.missiles[0] != null)
		{
			BombDragParachute componentInChildren = ml.missiles[0].GetComponentInChildren<BombDragParachute>(includeInactive: true);
			if ((bool)componentInChildren)
			{
				dragDelay = componentInChildren.deployDelay;
				initialDrag = componentInChildren.dragModule.area;
				deployedDrag = componentInChildren.deployedDrag;
			}
			gotChuteProps = true;
		}
	}

	public override void OnQuickloadEquip(ConfigNode eqNode)
	{
		base.OnQuickloadEquip(eqNode);
		TryGetChuteProperties();
	}

	public override Vector3 GetImpactPointWithLead(out float t)
	{
		bool flag = (bool)targeter && targeter.locked;
		float timeLimit = (flag ? 120 : 30);
		float simDeltaTime = (flag ? 0.1f : 0.2f);
		Vector3 targeterPosition = (flag ? targeter.lockTransform.position : Vector3.zero);
		_impactPoint = GetBombDragImpactPoint(out t, ml, simDeltaTime, timeLimit, bombMass, flag, targeterPosition);
		_leadTime = t;
		return _impactPoint;
	}

	public Vector3 GetBombDragImpactPoint(out float time, MissileLauncher ml, float simDeltaTime, float timeLimit, float bombMass, bool hasTargeter, Vector3 targeterPosition)
	{
		time = -1f;
		if (!ml.parentRb)
		{
			return Vector3.zero;
		}
		float num = 0f;
		Vector3 vector = ml.transform.position;
		Vector3 velocity = ml.parentRb.velocity;
		float num2 = -0.5f * simDeltaTime / bombMass;
		Ray ray = default(Ray);
		for (; num < timeLimit; num += simDeltaTime)
		{
			Vector3 vector2 = vector;
			velocity += Physics.gravity * simDeltaTime;
			float num3 = ((num < dragDelay) ? initialDrag : deployedDrag);
			velocity += num3 * num2 * AerodynamicsController.fetch.AtmosDensityAtPosition(vector2) * velocity.magnitude * velocity;
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
