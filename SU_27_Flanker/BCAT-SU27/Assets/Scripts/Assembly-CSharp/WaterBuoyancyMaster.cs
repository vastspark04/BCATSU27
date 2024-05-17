using UnityEngine;

public class WaterBuoyancyMaster : MonoBehaviour
{
	public WaterBuoyancy[] bPoints;

	private float radius;

	private bool bPointsEnabled;

	private Transform myTransform;

	private bool waterPhysicsReady;

	[HideInInspector]
	public bool bPointsDirty = true;

	private void Start()
	{
		GetBPoints();
		myTransform = base.transform;
	}

	private void GetBPoints()
	{
		bPoints = GetComponentsInChildren<WaterBuoyancy>(includeInactive: true);
		radius = 0f;
		for (int i = 0; i < bPoints.Length; i++)
		{
			float magnitude = base.transform.TransformVector(bPoints[i].transform.localPosition).magnitude;
			radius = Mathf.Max(radius, magnitude);
			bPoints[i].enabled = false;
		}
		bPointsEnabled = false;
		bPointsDirty = false;
	}

	private void Update()
	{
		if (waterPhysicsReady)
		{
			if (bPointsDirty)
			{
				GetBPoints();
			}
			if (myTransform.position.y - WaterPhysics.instance.height < radius)
			{
				if (!bPointsEnabled)
				{
					for (int i = 0; i < bPoints.Length; i++)
					{
						bPoints[i].enabled = true;
					}
					bPointsEnabled = true;
				}
			}
			else if (bPointsEnabled)
			{
				for (int j = 0; j < bPoints.Length; j++)
				{
					bPoints[j].enabled = false;
				}
				bPointsEnabled = false;
			}
		}
		else if ((bool)WaterPhysics.instance)
		{
			waterPhysicsReady = true;
		}
	}
}
