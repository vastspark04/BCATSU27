using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PSRateOverDistanceFix : MonoBehaviour
{
	private ParticleSystem ps;

	private ParticleSystem.EmissionModule em;

	private float rateOverDistance;

	private bool wasEnabled;

	private FixedPoint lastPoint;

	private Transform myTransform;

	private Transform customSpaceTf;

	private bool gotCustomSpace;

	private bool hasCustomSpace;

	private void Awake()
	{
		myTransform = base.transform;
		ps = GetComponent<ParticleSystem>();
		em = ps.emission;
		rateOverDistance = em.rateOverDistance.constant;
		em.rateOverDistance = 0f;
		em.rateOverTime = 0f;
	}

	private void Update()
	{
		if (em.enabled)
		{
			if (!gotCustomSpace)
			{
				gotCustomSpace = true;
				customSpaceTf = ps.main.customSimulationSpace;
				if ((bool)customSpaceTf)
				{
					hasCustomSpace = true;
				}
			}
			if (!wasEnabled)
			{
				wasEnabled = true;
				lastPoint = new FixedPoint(myTransform.position);
				return;
			}
			Vector3 vector = myTransform.position;
			Vector3 vector2 = lastPoint.point;
			Vector3 point = vector;
			if (hasCustomSpace)
			{
				vector = customSpaceTf.InverseTransformPoint(vector);
				vector2 = customSpaceTf.InverseTransformPoint(vector2);
			}
			Vector3 vector3 = vector - vector2;
			float magnitude = vector3.magnitude;
			Vector3 vector4 = vector3 / magnitude;
			int num = Mathf.FloorToInt(magnitude * rateOverDistance);
			if (num > 0)
			{
				float num2 = magnitude / (float)num;
				for (float num3 = 0f; num3 < magnitude; num3 += num2)
				{
					ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
					emitParams.position = vector2 + vector4 * num3;
					ps.Emit(emitParams, 1);
				}
				lastPoint.point = point;
			}
		}
		else
		{
			wasEnabled = false;
		}
	}
}
