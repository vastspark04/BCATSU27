using UnityEngine;

public class MARMGuidance : MissileGuidanceUnit
{
	public float turnInRadius;

	public float turnInAltitude;

	public float estimatedG = 60f;

	public float boosterRadius;

	public SolidBooster finalBooster;

	private bool boosted;

	private bool turned;

	protected override void OnBeginGuidance()
	{
		turnInRadius *= Random.Range(0.9f, 1.1f);
		turnInAltitude *= Random.Range(0.9f, 1.1f);
	}

	public override Vector3 GetGuidedPoint()
	{
		float num2;
		if ((bool)finalBooster)
		{
			float num = (finalBooster.thrust / base.missile.rb.mass + 4.905f) * finalBooster.burnTime;
			num2 = Mathf.Pow(base.missile.rb.velocity.magnitude + num, 2f);
		}
		else
		{
			num2 = base.missile.rb.velocity.sqrMagnitude;
		}
		turnInRadius = (turnInAltitude = num2 / (9.8f * estimatedG));
		if ((bool)finalBooster)
		{
			turnInRadius += 0.25f * (finalBooster.thrust / base.missile.rb.mass) * finalBooster.burnTime * finalBooster.burnTime;
		}
		boosterRadius = turnInRadius;
		Vector3 estTargetPos = base.missile.estTargetPos;
		Vector3 vector = estTargetPos - base.transform.position;
		vector.y = 0f;
		Vector3 vector2 = estTargetPos - vector.normalized * turnInRadius;
		vector2 += new Vector3(0f, turnInAltitude, 0f);
		if (!boosted && (bool)finalBooster && vector.sqrMagnitude < boosterRadius * boosterRadius)
		{
			boosted = true;
			finalBooster.Fire();
		}
		if (!turned && Vector3.Dot(vector, vector2 - base.transform.position) > 0f)
		{
			if (WaterPhysics.GetAltitude(base.transform.position) < WaterPhysics.GetAltitude(vector2))
			{
				return base.missile.transform.position + Quaternion.AngleAxis(-25f, Vector3.Cross(Vector3.up, vector)) * vector;
			}
			return base.missile.transform.position + vector;
		}
		turned = true;
		return base.missile.estTargetPos;
	}

	public override void SaveToQuicksaveNode(ConfigNode qsNode)
	{
		base.SaveToQuicksaveNode(qsNode);
		ConfigNode configNode = new ConfigNode("MARMGuidance");
		qsNode.AddNode(configNode);
		configNode.SetValue("turned", turned);
	}

	public override void LoadFromQuicksaveNode(Missile m, ConfigNode qsNode)
	{
		base.LoadFromQuicksaveNode(m, qsNode);
		string text = "MARMGuidance";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			turned = node.GetValue<bool>("turned");
		}
	}
}
