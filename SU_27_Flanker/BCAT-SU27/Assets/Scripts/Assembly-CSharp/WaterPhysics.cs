using UnityEngine;
using UnityEngine.Events;

public class WaterPhysics : MonoBehaviour
{
	private Transform myTransform;

	public static WaterPhysics instance { get; private set; }

	public float height => myTransform.position.y;

	public static float waterHeight
	{
		get
		{
			if ((bool)instance)
			{
				return instance.height;
			}
			return 0f;
		}
	}

	public Plane waterPlane => new Plane(Vector3.up, base.transform.position);

	public static event UnityAction OnWaterPhysicsChanged;

	public static float GetAltitude(Vector3 worldPos)
	{
		if (!instance)
		{
			return worldPos.y;
		}
		return worldPos.y - instance.height;
	}

	private void Awake()
	{
		instance = this;
		myTransform = base.transform;
	}

	private void OnEnable()
	{
		OnWaterPhysicsChanged += WaterPhysChanged;
		if (WaterPhysics.OnWaterPhysicsChanged != null)
		{
			WaterPhysics.OnWaterPhysicsChanged();
		}
	}

	private void OnDisable()
	{
		OnWaterPhysicsChanged -= WaterPhysChanged;
		if (instance == this)
		{
			instance = null;
		}
		if (WaterPhysics.OnWaterPhysicsChanged != null)
		{
			WaterPhysics.OnWaterPhysicsChanged();
		}
	}

	private void WaterPhysChanged()
	{
		if (base.enabled && base.gameObject.activeInHierarchy)
		{
			instance = this;
		}
	}
}
