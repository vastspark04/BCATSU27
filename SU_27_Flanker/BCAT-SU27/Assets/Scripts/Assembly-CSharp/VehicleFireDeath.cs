using UnityEngine;

public class VehicleFireDeath : MonoBehaviour, IQSVehicleComponent
{
	public GameObject firePrefab;

	public Transform fireTransform;

	public bool explosionNormalFwd;

	public ExplosionManager.ExplosionTypes explosionType;

	public GameObject customExplosion;

	private bool exploded;

	private bool spawnedFire;

	private void Awake()
	{
		if (!fireTransform)
		{
			fireTransform = base.transform;
		}
		Health componentInParent = GetComponentInParent<Health>();
		componentInParent.OnDeath.AddListener(OnDeath);
		VehiclePart component = componentInParent.GetComponent<VehiclePart>();
		if ((bool)component)
		{
			component.OnRepair.AddListener(OnRepair);
		}
	}

	private void OnRepair()
	{
		exploded = false;
	}

	private void OnDeath()
	{
		if (!exploded)
		{
			Vector3 vector = (explosionNormalFwd ? fireTransform.forward : Vector3.up);
			if ((bool)customExplosion)
			{
				Object.Instantiate(customExplosion, fireTransform.position, Quaternion.LookRotation(vector));
			}
			else
			{
				ExplosionManager.instance.CreateExplosionEffect(explosionType, fireTransform.position, vector);
			}
			exploded = true;
		}
		SpawnFire();
	}

	private void SpawnFire()
	{
		if ((bool)firePrefab && !spawnedFire)
		{
			spawnedFire = true;
			GameObject gameObject = Object.Instantiate(firePrefab, fireTransform.position, fireTransform.rotation, fireTransform);
			HeatFire component = gameObject.GetComponent<HeatFire>();
			if ((bool)component)
			{
				component.target = fireTransform;
				gameObject.transform.parent = null;
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_VehicleFireDeath");
		configNode.SetValue("exploded", exploded);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_VehicleFireDeath";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			exploded = node.GetValue<bool>("exploded");
			if (exploded)
			{
				SpawnFire();
			}
		}
	}
}
