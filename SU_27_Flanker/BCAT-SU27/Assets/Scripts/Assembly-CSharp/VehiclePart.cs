using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Health))]
public class VehiclePart : MonoBehaviour, IQSVehicleComponent
{
	private enum PartStates
	{
		Normal,
		BeganDetachDelay,
		Detached,
		KilledSubComponents,
		Exploded
	}

	public enum EqJettisonModes
	{
		None,
		All,
		Random
	}

	[Serializable]
	public class DamageStepEvent
	{
		public float minNormalizedHealth;

		public UnityEvent damageEvent;

		[HideInInspector]
		public bool hasFired;
	}

	public string partName = "Part";

	public VehiclePart parent;

	public float partMass = 0.1f;

	public bool detachOnDeath;

	public MinMax detachDelay;

	public bool explodeAfterDeath;

	public MinMax explosionDelay;

	public ExplosionManager.ExplosionTypes explosionType;

	public bool immediateDetachChildrenOnDeath;

	private Health _h;

	private bool _gotH;

	private Rigidbody rb;

	private MassUpdater parentMassUpdater;

	private WeaponManager parentWeaponManager;

	public DamageStepEvent[] damageEvents;

	public UnityEvent OnPartDetach;

	private PartStates partState;

	private Vector3 origLocalPos;

	private Quaternion origLocalRot;

	private Transform origParentTf;

	public bool jettisonEquipment = true;

	public EqJettisonModes equipmentJettison = EqJettisonModes.Random;
	
	[Header("G Damage")]
	public bool doGDamage;

	public float damagePerGLimit;

	public MinMax gDamageRand = new MinMax(1f, 1f);

	public float maxGDamage;


	public bool killSubComponentsOnDeath = true;

	public float impactVelocityExplosionThreshold = 10f;

	public UnityEvent OnRepair;

	public List<VehiclePart> children { get; private set; }

	public Health health
	{
		get
		{
			if (!_gotH)
			{
				_h = GetComponent<Health>();
				_gotH = true;
			}
			return _h;
		}
	}

	public bool hasDetached { get; private set; }

	public bool partDied { get; private set; }

	private void Awake()
	{
		if (children == null)
		{
			children = new List<VehiclePart>();
		}
		if ((bool)parent)
		{
			parent.AddChild(this);
			origParentTf = base.transform.parent;
			origLocalPos = base.transform.localPosition;
			origLocalRot = base.transform.localRotation;
		}
		health.OnDeath.AddListener(OnDeath);
		health.OnDamage += Health_OnDamage;
		parentMassUpdater = GetComponentInParent<MassUpdater>();
		parentWeaponManager = GetComponentInParent<WeaponManager>();
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		FlightSceneManager.instance.OnExitScene += OnExitScene;
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
		if (children == null)
		{
			return;
		}
		foreach (VehiclePart child in children)
		{
			if ((bool)child)
			{
				UnityEngine.Object.Destroy(child.gameObject, UnityEngine.Random.Range(0f, 5f));
			}
		}
	}

	private void OnExitScene()
	{
		if ((bool)base.gameObject)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void Health_OnDamage(float damage, Vector3 position, Health.DamageTypes damageType)
	{
		float normalizedHealth = health.normalizedHealth;
		DamageStepEvent[] array = damageEvents;
		foreach (DamageStepEvent damageStepEvent in array)
		{
			if (!damageStepEvent.hasFired && normalizedHealth <= damageStepEvent.minNormalizedHealth)
			{
				damageStepEvent.damageEvent.Invoke();
				damageStepEvent.hasFired = true;
			}
		}
	}

	private void OnDeath()
	{
		partDied = true;
		if ((bool)parent && detachOnDeath)
		{
			StartCoroutine(DetachRoutine());
		}
		else if (killSubComponentsOnDeath)
		{
			KillSubComponents();
		}
	}

	public void RemoteDetachPart()
	{
		if (!hasDetached)
		{
			detachOnDeath = true;
			detachDelay = new MinMax(0f, 0f);
			OnDeath();
		}
	}

	private void KillSubComponents()
	{
		if (!this || partState == PartStates.KilledSubComponents)
		{
			return;
		}
		if (partState != PartStates.Exploded)
		{
			partState = PartStates.KilledSubComponents;
		}
		if (children != null)
		{
			foreach (VehiclePart child in children)
			{
				if ((bool)child && !child.GetComponent<HardpointVehiclePart>())
				{
					child.Kill(health ? health.killedByActor : null);
					if (immediateDetachChildrenOnDeath && child.detachOnDeath)
					{
						FloatingOrigin.instance.AddQueuedFixedUpdateAction(child.FinalDetach);
					}
				}
			}
		}
		if (jettisonEquipment)
		{
			HPEquippable[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<HPEquippable>();
			foreach (HPEquippable hPEquippable in componentsInChildrenImplementing)
			{
				if ((bool)hPEquippable.weaponManager)
				{
					if (hPEquippable.jettisonable && (equipmentJettison == EqJettisonModes.All || (equipmentJettison == EqJettisonModes.Random && UnityEngine.Random.Range(0f, 1f) < 0.26f)))
					{
						hPEquippable.weaponManager.JettisonByPartDestruction(hPEquippable.hardpointIdx);
					}
					else
					{
						hPEquippable.weaponManager.DisableWeaponByPartDestruction(hPEquippable.hardpointIdx);
					}
				}
			}
		}
		if ((bool)parentWeaponManager)
		{
			parentWeaponManager.RefreshWeapon();
		}
		if (explodeAfterDeath && base.gameObject.activeInHierarchy)
		{
			StartCoroutine(ExplodeRoutine());
		}
	}

	public void JettisonAttachedEquips()
	{
		HPEquippable[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<HPEquippable>();
		foreach (HPEquippable hPEquippable in componentsInChildrenImplementing)
		{
			if (hPEquippable.jettisonable)
			{
				hPEquippable.weaponManager.JettisonByPartDestruction(hPEquippable.hardpointIdx);
			}
			else
			{
				hPEquippable.weaponManager.DisableWeaponByPartDestruction(hPEquippable.hardpointIdx);
			}
		}
	}

	private IEnumerator DetachRoutine()
	{
		if (partState != PartStates.Exploded)
		{
			partState = PartStates.BeganDetachDelay;
		}
		yield return new WaitForSeconds(detachDelay.Random());
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(FinalDetach);
	}

	private void FinalDetach()
	{
		if (hasDetached)
		{
			return;
		}
		hasDetached = true;
		if (partState != PartStates.Exploded)
		{
			partState = PartStates.Detached;
		}
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.isKinematic = false;
		rb.velocity = parent.GetPointVelocity(rb.position);
		if ((bool)parent.rb)
		{
			rb.angularVelocity = parent.rb.angularVelocity;
		}
		rb.interpolation = RigidbodyInterpolation.Interpolate;
		rb.mass = partMass;
		base.transform.parent = null;
		base.gameObject.AddComponent<FloatingOriginTransform>();
		if ((bool)parentMassUpdater)
		{
			parentMassUpdater.UpdateMassObjects();
		}
		IParentRBDependent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
		foreach (IParentRBDependent parentRBDependent in componentsInChildrenImplementing)
		{
			parentRBDependent.SetParentRigidbody(rb);
			if (parentRBDependent is SimpleDrag)
			{
				((SimpleDrag)parentRBDependent).enabled = true;
			}
		}
		KillSubComponents();
		if (OnPartDetach != null)
		{
			OnPartDetach.Invoke();
		}
	}

	private Vector3 GetPointVelocity(Vector3 point)
	{
		if ((bool)rb && !rb.isKinematic)
		{
			return rb.GetPointVelocity(point);
		}
		if ((bool)parent)
		{
			return parent.GetPointVelocity(point);
		}
		if ((bool)rb)
		{
			return rb.velocity;
		}
		return Vector3.zero;
	}

	public void Kill(Actor sourceActor)
	{
		health.Damage(health.maxHealth, base.transform.position, Health.DamageTypes.Impact, sourceActor);
	}

	public void RemoteKill(Actor sourceActor)
	{
		if (!partDied)
		{
			health.Damage(health.maxHealth, base.transform.position, Health.DamageTypes.Impact, sourceActor, null, rpcIfRemote: false);
		}
	}

	private IEnumerator ExplodeRoutine()
	{
		float delay = explosionDelay.Random();
		float thresholdSqr = impactVelocityExplosionThreshold * impactVelocityExplosionThreshold;
		Vector3 lastVel = Vector3.zero;
		if ((bool)rb)
		{
			lastVel = rb.velocity;
		}
		for (float t = 0f; t < delay; t += Time.deltaTime)
		{
			if (base.transform.position.y < WaterPhysics.instance.height)
			{
				break;
			}
			if ((bool)rb)
			{
				Vector3 velocity = rb.velocity;
				if (t > 0.2f && (velocity - lastVel).sqrMagnitude > thresholdSqr)
				{
					break;
				}
				lastVel = velocity;
			}
			yield return new WaitForFixedUpdate();
		}
		ExplosionManager.instance.CreateExplosionEffect(explosionType, base.transform.position, rb ? rb.velocity : base.transform.forward);
		partState = PartStates.Exploded;
		base.gameObject.SetActive(value: false);
	}

	private void AddChild(VehiclePart c)
	{
		if (children == null)
		{
			children = new List<VehiclePart>();
		}
		children.Add(c);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(base.gameObject.name + "_VehiclePart");
		configNode.SetValue("partState", partState);
		configNode.SetValue("hasDetached", hasDetached);
		if (hasDetached)
		{
			configNode.SetValue("globalPos", VTMapManager.WorldToGlobalPoint(rb.position));
			configNode.SetValue("velocity", rb.velocity);
			configNode.SetValue("rotation", rb.rotation.eulerAngles);
			configNode.SetValue("angularVelocity", rb.angularVelocity);
		}
		foreach (VehiclePart child in children)
		{
			if (child.hasDetached)
			{
				child.OnQuicksave(qsNode);
			}
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(base.gameObject.name + "_VehiclePart");
		if (node == null)
		{
			return;
		}
		PartStates partStates = (partState = node.GetValue<PartStates>("partState"));
		if (node.GetValue<bool>("hasDetached"))
		{
			FinalDetach();
			if ((bool)rb)
			{
				Vector3 vector3 = (rb.position = (base.transform.position = VTMapManager.GlobalToWorldPoint(node.GetValue<Vector3D>("globalPos"))));
				rb.velocity = node.GetValue<Vector3>("velocity");
				Quaternion quaternion3 = (rb.rotation = (base.transform.rotation = Quaternion.Euler(node.GetValue<Vector3>("rotation"))));
				rb.angularVelocity = node.GetValue<Vector3>("angularVelocity");
			}
		}
		switch (partState)
		{
		case PartStates.BeganDetachDelay:
			StartCoroutine(DetachRoutine());
			break;
		case PartStates.KilledSubComponents:
			KillSubComponents();
			break;
		case PartStates.Exploded:
			KillSubComponents();
			base.gameObject.SetActive(value: false);
			break;
		case PartStates.Normal:
		case PartStates.Detached:
			break;
		}
	}

	[ContextMenu("Repair")]
	public void Repair()
	{
		if ((bool)parent && (bool)origParentTf)
		{
			base.transform.parent = origParentTf;
			base.transform.localPosition = origLocalPos;
			base.transform.localRotation = origLocalRot;
		}
		if ((bool)health)
		{
			health.Heal(health.maxHealth);
		}
		if (partState == PartStates.Exploded)
		{
			base.gameObject.SetActive(value: true);
		}
		if (hasDetached)
		{
			if ((bool)rb)
			{
				UnityEngine.Object.Destroy(rb);
			}
			Rigidbody parentRigidbody = GetRootPart().rb;
			IParentRBDependent[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IParentRBDependent>(includeInactive: true);
			foreach (IParentRBDependent parentRBDependent in componentsInChildrenImplementing)
			{
				parentRBDependent.SetParentRigidbody(parentRigidbody);
				if (parentRBDependent is SimpleDrag)
				{
					((SimpleDrag)parentRBDependent).enabled = false;
				}
			}
			if ((bool)parentMassUpdater)
			{
				parentMassUpdater.UpdateMassObjects();
			}
			FloatingOriginTransform component = GetComponent<FloatingOriginTransform>();
			if ((bool)component)
			{
				UnityEngine.Object.Destroy(component);
			}
			hasDetached = false;
			HPEquippable[] componentsInChildrenImplementing2 = base.gameObject.GetComponentsInChildrenImplementing<HPEquippable>(includeInactive: true);
			foreach (HPEquippable hPEquippable in componentsInChildrenImplementing2)
			{
				if ((bool)hPEquippable && !hPEquippable.enabled)
				{
					UnityEngine.Object.Destroy(hPEquippable.gameObject);
				}
			}
		}
		partState = PartStates.Normal;
		partDied = false;
		if (OnRepair != null)
		{
			OnRepair.Invoke();
		}
		foreach (VehiclePart child in children)
		{
			child.Repair();
		}
	}

	public VehiclePart GetRootPart()
	{
		VehiclePart vehiclePart = this;
		while ((bool)vehiclePart.parent)
		{
			vehiclePart = vehiclePart.parent;
		}
		return vehiclePart;
	}
}
