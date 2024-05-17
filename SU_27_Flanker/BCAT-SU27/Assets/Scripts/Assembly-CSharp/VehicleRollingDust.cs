using UnityEngine;

public class VehicleRollingDust : MonoBehaviour
{
	public RaySpringDamper suspension;

	public GroundUnitMover mover;

	public ParticleAnimator particleAnimator;

	private Collider moverSurfColl;

	private WheelSurfaceMaterial wsMat;

	private bool useSusp;

	private float lastSqrSpeed = -1f;

	private void Awake()
	{
		if ((bool)suspension)
		{
			useSusp = true;
		}
	}

	private void Update()
	{
		float num = 0f;
		WheelSurfaceMaterial wheelSurfaceMaterial = null;
		if (useSusp)
		{
			wsMat = suspension.surfaceMaterial;
			num = suspension.surfaceVelocity.sqrMagnitude;
			wheelSurfaceMaterial = wsMat;
		}
		else
		{
			RaycastHit hitInfo;
			if (mover.enabled)
			{
				if (moverSurfColl != mover.onCollider)
				{
					moverSurfColl = mover.onCollider;
					if (!WheelSurface.TryGetMaterial(moverSurfColl, out wsMat))
					{
						wsMat = null;
					}
				}
			}
			else if (Physics.Raycast(mover.transform.position - mover.height * mover.transform.up + new Vector3(0f, 2f, 0f), Vector3.down, out hitInfo, 4f, 1, QueryTriggerInteraction.Ignore) && !WheelSurface.TryGetMaterial(hitInfo.collider, out wsMat))
			{
				wsMat = null;
			}
			wheelSurfaceMaterial = wsMat;
			if ((bool)wsMat)
			{
				if (VTMapManager.IsPositionOverCityStreet(base.transform.position))
				{
					wheelSurfaceMaterial = null;
				}
				num = mover.velocity.sqrMagnitude;
			}
		}
		if ((bool)wheelSurfaceMaterial && wheelSurfaceMaterial.dustColor.a > 0.01f)
		{
			ParticleSystem.MainModule main = particleAnimator.ps.main;
			main.startColor = new ParticleSystem.MinMaxGradient(wheelSurfaceMaterial.dustColor);
			if (Mathf.Abs(num - lastSqrSpeed) > 0.1f)
			{
				lastSqrSpeed = num;
				particleAnimator.Evaluate(Mathf.Sqrt(num));
			}
		}
		else
		{
			particleAnimator.Evaluate(0f);
			lastSqrSpeed = -1f;
		}
	}
}
