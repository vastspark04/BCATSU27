using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : HPEquippable, IMassObject, IParentRBDependent, IRippleWeapon, ILocalizationUser
{
	public float launcherMass;

	public GameObject rocketPrefab;

	public Transform[] fireTransforms;

	public float perRocketCost = 10f;

	public float shakeMagnitude = -1f;

	private Rocket[] rockets;

	private int rocketCount;

	private Transform _aiAimTransform;

	private int currentIdx;

	private FixedPoint _impactPoint;

	private float impactTime;

	private float impactDistance;

	private float rocketInitialSpeed;

	private float rocketThrust;

	private float rocketThrustDecayFactor;

	private float rocketThrustTime;

	private float rocketMass;

	private Vector3 tgtVel;

	public AudioSource launchAudioSource;

	public AudioClip launchAudioClip;

	public Transform proxyModelTf;

	public bool useProxyModel;

	public float[] rippleRates = new float[4] { 0f, 800f, 2000f, 4000f };

	private int rippleIdx;

	public bool autoCalcImpact = true;

	public Rigidbody vesselRB;

	private Actor parentActor;

	private float rocketDamage;

	[Header("AutoAlign")]
	public Transform autoAlignTransform;

	public Vector2 autoAlignRotation;

	private Vector3 fireTfLocalPos;

	private string s_rocket_salvo;

	private List<Rocket> myFiredRockets = new List<Rocket>();

	private bool hasPredictedImpact;

	private bool doShake;

	private int maxSalvo = 4;

	private List<RocketLauncher> salvoLaunchers = new List<RocketLauncher>();

	private Vector3 impactPoint
	{
		get
		{
			return _impactPoint.point;
		}
		set
		{
			_impactPoint = new FixedPoint(value);
		}
	}

	public Rocket lastFiredRocket
	{
		get
		{
			if (myFiredRockets.Count > 0)
			{
				return myFiredRockets[myFiredRockets.Count - 1];
			}
			return null;
		}
	}

	public int liveRocketsCount => myFiredRockets.Count;

	public int salvoCount { get; private set; } = 1;


	public event Action<Rocket> OnFiredRocket;

	public event Action OnReloaded;

	public override float GetTotalCost()
	{
		return unitCost + perRocketCost * (float)rocketCount;
	}

	public Transform GetAIAimTransform()
	{
		if (!_aiAimTransform)
		{
			_aiAimTransform = new GameObject("aimTf").transform;
			_aiAimTransform.parent = base.transform;
			_aiAimTransform.position = fireTransforms[0].position;
			_aiAimTransform.rotation = Quaternion.LookRotation(fireTransforms[0].forward, base.weaponManager.transform.up);
		}
		return _aiAimTransform;
	}

	public void SetParentActor(Actor a)
	{
		parentActor = a;
	}

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		s_rocket_salvo = VTLocalizationManager.GetString("s_rocket_salvo", "SALVO", "Rocket launcher salvo option");
	}

	protected override void Awake()
	{
		base.Awake();
		if (rockets == null)
		{
			rockets = new Rocket[fireTransforms.Length];
		}
		if (useProxyModel && !proxyModelTf)
		{
			useProxyModel = false;
		}
		fireTfLocalPos = base.transform.InverseTransformPoint(fireTransforms[0].position);
		ReloadAll();
	}

	protected override void Start()
	{
		base.Start();
		_impactPoint = default(FixedPoint);
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		vesselRB = rb;
	}

	protected override void SaveEquipData(ConfigNode weaponNode)
	{
		base.SaveEquipData(weaponNode);
		weaponNode.SetValue("rippleIdx", rippleIdx);
		weaponNode.SetValue("salvoCount", salvoCount);
	}

	protected override void LoadEquipData(ConfigNode weaponNode)
	{
		base.LoadEquipData(weaponNode);
		ConfigNodeUtils.TryParseValue(weaponNode, "rippleIdx", ref rippleIdx);
		int target = salvoCount;
		ConfigNodeUtils.TryParseValue(weaponNode, "salvoCount", ref target);
		salvoCount = target;
	}

	public override void OnEnableWeapon()
	{
		base.OnEnableWeapon();
		StartCoroutine(ItemActivatedRoutine());
	}

	private IEnumerator ItemActivatedRoutine()
	{
		while (base.itemActivated)
		{
			if (autoCalcImpact)
			{
				if ((bool)base.weaponManager && (bool)base.weaponManager.opticalTargeter && base.weaponManager.opticalTargeter.locked && !base.weaponManager.opticalTargeter.lockedSky)
				{
					hasPredictedImpact = CalculateImpactWithTarget(base.weaponManager.opticalTargeter.lockTransform.position, 0.04f, 16f, out impactTime, out impactDistance, out var hitPos);
					impactPoint = hitPos;
					tgtVel = base.weaponManager.opticalTargeter.targetVelocity;
				}
				else
				{
					impactPoint = CalculateImpact(out impactTime);
					impactDistance = (fireTransforms[0].position - impactPoint).magnitude;
					tgtVel = Vector3.zero;
				}
			}
			if ((bool)base.dlz)
			{
				if ((bool)base.weaponManager.opticalTargeter && base.weaponManager.opticalTargeter.locked)
				{
					base.dlz.UpdateLaunchParams(base.transform.TransformPoint(fireTfLocalPos), base.weaponManager.vesselRB.velocity, base.weaponManager.opticalTargeter.lockTransform.position, base.weaponManager.opticalTargeter.targetVelocity);
				}
				else if (hasPredictedImpact)
				{
					base.dlz.UpdateLaunchParams(base.transform.TransformPoint(fireTfLocalPos), base.weaponManager.vesselRB.velocity, impactPoint, Vector3.zero);
				}
				else
				{
					base.dlz.SetNoTarget();
				}
			}
			yield return null;
		}
	}

	private void LateUpdate()
	{
		if (useProxyModel)
		{
			proxyModelTf.position = base.transform.position;
			proxyModelTf.rotation = base.transform.rotation;
		}
	}

	public override bool LaunchAuthorized()
	{
		bool flag = hasPredictedImpact && rocketCount > 0 && (bool)base.dlz && base.dlz.inRangeMax;
		if ((bool)base.weaponManager && (bool)base.weaponManager.opticalTargeter)
		{
			OpticalTargeter opticalTargeter = base.weaponManager.opticalTargeter;
			if (flag && opticalTargeter.locked && !opticalTargeter.lockedSky)
			{
				return Vector3.Dot((opticalTargeter.lockTransform.position - base.transform.position).normalized, (GetAimPoint() - base.transform.position).normalized) > 0.9998f;
			}
			return false;
		}
		return flag;
	}

	[ContextMenu("Fire")]
	public bool FireRocket()
	{
		if (rockets[currentIdx] == null)
		{
			int num = currentIdx;
			do
			{
				currentIdx = (currentIdx + 1) % rockets.Length;
			}
			while (currentIdx != num && !(rockets[currentIdx] != null));
		}
		if (rockets[currentIdx] == null)
		{
			return false;
		}
		Rocket rocket = rockets[currentIdx];
		rocket.launcherRB = vesselRB;
		rocket.Fire(parentActor);
		myFiredRockets.Add(rocket);
		rocket.OnDetonated += OnRocketDetonated;
		rocketCount--;
		rockets[currentIdx] = null;
		if ((bool)launchAudioSource)
		{
			launchAudioSource.Stop();
			launchAudioSource.PlayOneShot(launchAudioClip);
		}
		currentIdx = (currentIdx + 1) % rockets.Length;
		this.OnFiredRocket?.Invoke(rocket);
		if (doShake && shakeMagnitude > 0f)
		{
			CamRigRotationInterpolator.ShakeAll(UnityEngine.Random.onUnitSphere * shakeMagnitude);
		}
		return true;
	}

	public void MP_FireRocket(Vector3 position, Vector3 direction)
	{
		if (rockets[currentIdx] == null)
		{
			int num = currentIdx;
			do
			{
				currentIdx = (currentIdx + 1) % rockets.Length;
			}
			while (currentIdx != num && !(rockets[currentIdx] != null));
		}
		if (!(rockets[currentIdx] == null))
		{
			Rocket rocket = rockets[currentIdx];
			rocket.transform.position = position;
			rocket.transform.rotation = Quaternion.LookRotation(direction, rocket.transform.up);
			rocket.launcherRB = vesselRB;
			rocket.Fire(parentActor);
			myFiredRockets.Add(rocket);
			rocket.damage = 0f;
			rocket.OnDetonated += OnRocketDetonated;
			rocketCount--;
			rockets[currentIdx] = null;
			if ((bool)launchAudioSource)
			{
				launchAudioSource.Stop();
				launchAudioSource.PlayOneShot(launchAudioClip);
			}
			currentIdx = (currentIdx + 1) % rockets.Length;
		}
	}

	private void OnRocketDetonated(Rocket r)
	{
		myFiredRockets.Remove(r);
	}

	[ContextMenu("Reload")]
	public void ReloadAll()
	{
		for (int i = 0; i < fireTransforms.Length; i++)
		{
			LoadRocket();
		}
	}

	public void LoadCount(int count)
	{
		if (rockets == null)
		{
			rockets = new Rocket[fireTransforms.Length];
		}
		int i;
		for (i = 0; i < count && i < fireTransforms.Length; i++)
		{
			if (rockets[i] == null)
			{
				LoadRocket(i);
			}
		}
		for (; i < fireTransforms.Length; i++)
		{
			if (rockets[i] != null)
			{
				UnityEngine.Object.Destroy(rockets[i].gameObject);
				rockets[i] = null;
			}
		}
		rocketCount = Mathf.Min(count, fireTransforms.Length);
	}

	public void LoadRocket()
	{
		if (rockets == null)
		{
			rockets = new Rocket[fireTransforms.Length];
		}
		for (int i = 0; i < fireTransforms.Length; i++)
		{
			if (rockets[i] == null)
			{
				LoadRocket(i);
			}
		}
	}

	private void LoadRocket(int idx)
	{
		if (rockets == null)
		{
			rockets = new Rocket[fireTransforms.Length];
		}
		Rocket newRocketInstance = GetNewRocketInstance(fireTransforms[idx]);
		rockets[idx] = newRocketInstance;
		rocketInitialSpeed = newRocketInstance.initialKickVel;
		rocketThrust = newRocketInstance.thrust;
		rocketThrustDecayFactor = newRocketInstance.thrustDecayFactor;
		rocketThrustTime = newRocketInstance.thrustTime;
		rocketMass = newRocketInstance.mass;
		rocketDamage = newRocketInstance.damage;
		rocketCount++;
	}

	private Rocket GetNewRocketInstance(Transform parent)
	{
		GameObject obj = UnityEngine.Object.Instantiate(rocketPrefab);
		obj.SetActive(value: true);
		obj.transform.parent = parent;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localScale = Vector3.one;
		return obj.GetComponent<Rocket>();
	}

	public override float GetWeaponDamage()
	{
		return rocketDamage;
	}

	public override int GetCount()
	{
		return rocketCount;
	}

	public override int GetMaxCount()
	{
		return fireTransforms.Length;
	}

	public override Vector3 GetAimPoint()
	{
		return impactPoint - tgtVel * impactTime;
	}

	public float GetImpactDistance()
	{
		return impactDistance;
	}

	public float GetImpactTime()
	{
		return impactTime;
	}

	public override void OnStartFire()
	{
		if (salvoCount == 1)
		{
			if (FireRocket())
			{
				base.weaponManager.ToggleCombinedWeapon();
			}
		}
		else
		{
			FireSalvo();
			base.weaponManager.ToggleCombinedWeapon();
		}
	}

	private Vector3 CalculateImpact(out float _impactTime, float maxTime = 8f)
	{
		_impactTime = 0f;
		Vector3 result = Vector3.zero;
		if (!vesselRB)
		{
			return result;
		}
		Vector3 vector = base.transform.TransformPoint(fireTfLocalPos);
		float num = 0.02f;
		float num2 = rocketThrust;
		float num3 = num / rocketMass;
		Vector3 pointVelocity = vesselRB.GetPointVelocity(vector);
		Vector3 vector2 = fireTransforms[currentIdx].forward;
		pointVelocity += vector2 * (rocketInitialSpeed + num2 * num3);
		Vector3 vector3 = vector;
		float num4 = 0f;
		hasPredictedImpact = false;
		float num5 = Physics.gravity.y * num;
		for (; num4 < 8f; num4 += num)
		{
			Vector3 vector4 = vector3 + pointVelocity * num;
			if (Physics.Linecast(vector3, vector4, out var hitInfo, 1))
			{
				result = hitInfo.point;
				hasPredictedImpact = true;
				break;
			}
			if (num4 < rocketThrustTime)
			{
				float num6 = num2 * num3;
				num2 -= num2 * rocketThrustDecayFactor * num;
				pointVelocity += num6 * vector2;
				vector2 = Vector3.RotateTowards(vector2, pointVelocity, Rocket.GetRotationDelta(num4, vector4, pointVelocity, num) * ((float)Math.PI / 180f), 0f);
			}
			pointVelocity.y += num5;
			vector3 = vector4;
		}
		if (!hasPredictedImpact)
		{
			result = vector3;
		}
		_impactTime = num4;
		return result;
	}

	public bool CalculateImpactWithTarget(Vector3 targetPosition, float simDeltaTime, float simMaxTime, out float time, out float tgtDist, out Vector3 hitPos)
	{
		Vector3 vector = base.transform.TransformPoint(fireTfLocalPos);
		float num = simDeltaTime / rocketMass;
		float num2 = rocketThrust;
		Vector3 pointVelocity = vesselRB.GetPointVelocity(vector);
		Vector3 vector2 = fireTransforms[currentIdx].forward;
		pointVelocity += vector2 * rocketInitialSpeed;
		Vector3 vector3 = vector;
		float num3 = 0f;
		tgtDist = (targetPosition - vector).magnitude;
		hitPos = vector + fireTransforms[0].forward * tgtDist;
		float num4 = Physics.gravity.y * simDeltaTime;
		bool result = false;
		while (num3 < simMaxTime)
		{
			Vector3 vector4 = vector3;
			if (num3 < rocketThrustTime)
			{
				float num5 = Mathf.Min(rocketThrustTime - num3, simDeltaTime) / simDeltaTime;
				float num6 = num2 * num;
				num2 -= num2 * rocketThrustDecayFactor * simDeltaTime * num5;
				Vector3 vector5 = num6 * num5 * vector2;
				pointVelocity += vector5;
				vector4 += simDeltaTime * num5 * vector5;
				vector2 = Vector3.RotateTowards(vector2, pointVelocity, Rocket.GetRotationDelta(num3, vector4, pointVelocity, simDeltaTime) * ((float)Math.PI / 180f), 0f);
			}
			if ((vector4 - vector).sqrMagnitude > tgtDist * tgtDist)
			{
				hitPos = vector + (vector4 - vector).normalized * tgtDist;
				result = true;
				break;
			}
			pointVelocity.y += num4;
			num3 += simDeltaTime;
			hitPos = vector4;
			vector4 += pointVelocity * simDeltaTime;
			vector4.y += num4 * simDeltaTime;
			vector3 = vector4;
		}
		time = num3;
		return result;
	}

	public float GetMass()
	{
		return (float)rocketCount * rocketMass + launcherMass;
	}

	public float[] GetRippleRates()
	{
		return rippleRates;
	}

	public void SetRippleRateIdx(int idx)
	{
		rippleIdx = idx;
	}

	public int GetRippleRateIdx()
	{
		return rippleIdx;
	}

	protected override void OnEquip()
	{
		base.OnEquip();
		parentActor = base.weaponManager.actor;
		if ((bool)autoAlignTransform)
		{
			Vector3 forward = base.weaponManager.transform.forward;
			forward = Quaternion.AngleAxis(autoAlignRotation.x, base.weaponManager.transform.right) * forward;
			forward = Quaternion.AngleAxis(autoAlignRotation.y, Vector3.Cross(forward, base.weaponManager.transform.right)) * forward;
			autoAlignTransform.rotation = Quaternion.LookRotation(forward, autoAlignTransform.up);
		}
		if (useProxyModel)
		{
			if ((bool)proxyModelTf)
			{
				proxyModelTf.parent = null;
			}
			else
			{
				useProxyModel = false;
			}
		}
		EquipFunction equipFunction = new EquipFunction();
		equipFunction.optionEvent = (EquipFunction.OptionEvent)Delegate.Combine(equipFunction.optionEvent, new EquipFunction.OptionEvent(ToggleSalvo));
		equipFunction.optionName = s_rocket_salvo;
		equipFunction.optionReturnLabel = salvoCount.ToString();
		equipFunctions = new EquipFunction[1] { equipFunction };
		if (base.weaponManager.isPlayer)
		{
			doShake = true;
		}
	}

	protected override void OnJettison()
	{
		base.OnJettison();
		if (useProxyModel)
		{
			proxyModelTf.parent = base.transform;
			proxyModelTf.localRotation = Quaternion.identity;
			proxyModelTf.localPosition = Vector3.one;
		}
		StartCoroutine(UnloadDelayed(5f));
	}

	private IEnumerator UnloadDelayed(float time)
	{
		yield return new WaitForSeconds(time);
		if (rockets == null)
		{
			yield break;
		}
		for (int i = 0; i < rockets.Length; i++)
		{
			if (rockets[i] != null)
			{
				UnityEngine.Object.Destroy(rockets[i].gameObject);
				rockets[i] = null;
			}
		}
		rocketCount = 0;
	}

	private void OnEnable()
	{
		if (useProxyModel)
		{
			if ((bool)proxyModelTf)
			{
				proxyModelTf.gameObject.SetActive(value: true);
			}
			else
			{
				useProxyModel = false;
			}
		}
	}

	private void OnDisable()
	{
		if (!useProxyModel)
		{
			return;
		}
		if ((bool)proxyModelTf)
		{
			if (base.gameObject.activeInHierarchy)
			{
				proxyModelTf.parent = base.transform;
				proxyModelTf.localPosition = Vector3.zero;
				proxyModelTf.localRotation = Quaternion.identity;
			}
			else
			{
				proxyModelTf.gameObject.SetActive(value: false);
			}
		}
		else
		{
			useProxyModel = false;
		}
	}

	public override float GetEstimatedMass()
	{
		return launcherMass + (float)fireTransforms.Length * rocketPrefab.GetComponent<Rocket>().mass;
	}

	private void OnDestroy()
	{
		if ((bool)proxyModelTf)
		{
			UnityEngine.Object.Destroy(proxyModelTf.gameObject);
		}
	}

	public override void OnDisableWeapon()
	{
		base.OnDisableWeapon();
		if ((bool)base.dlz)
		{
			base.dlz.SetNoTarget();
		}
	}

	private string ToggleSalvo()
	{
		maxSalvo = 0;
		for (int i = 0; i < base.weaponManager.equipCount; i++)
		{
			HPEquippable equip = base.weaponManager.GetEquip(i);
			if ((bool)equip && equip is RocketLauncher && equip.shortName == shortName)
			{
				maxSalvo++;
			}
		}
		if (maxSalvo == 0)
		{
			maxSalvo = 1;
		}
		salvoCount++;
		if (salvoCount > maxSalvo)
		{
			salvoCount = 1;
		}
		return salvoCount.ToString();
	}

	private void FireSalvo()
	{
		salvoLaunchers.Clear();
		for (int i = 0; i < base.weaponManager.equipCount; i++)
		{
			HPEquippable equip = base.weaponManager.GetEquip(i);
			if ((bool)equip && equip is RocketLauncher && equip.GetCount() > 0 && equip.shortName == shortName)
			{
				salvoLaunchers.Add((RocketLauncher)equip);
			}
		}
		salvoLaunchers.Sort(delegate(RocketLauncher a, RocketLauncher b)
		{
			float num2 = Vector3.Dot(a.transform.position - base.weaponManager.transform.position, base.weaponManager.transform.right);
			float value = Vector3.Dot(b.transform.position - base.weaponManager.transform.position, base.weaponManager.transform.right);
			return num2.CompareTo(value);
		});
		int num = 0;
		bool flag = false;
		while (num < salvoCount && salvoLaunchers.Count > 0)
		{
			int index = (flag ? (salvoLaunchers.Count - 1) : 0);
			bool flag2 = false;
			while (!flag2 && salvoLaunchers.Count > 0)
			{
				if (salvoLaunchers[index].FireRocket())
				{
					flag2 = true;
					num++;
				}
				salvoLaunchers.RemoveAt(index);
				if (flag)
				{
					index = salvoLaunchers.Count - 1;
				}
			}
			flag = !flag;
		}
	}

	public override void OnQuicksaveEquip(ConfigNode eqNode)
	{
		base.OnQuicksaveEquip(eqNode);
		eqNode.SetValue("rocketCount", rocketCount);
		for (int i = 0; i < myFiredRockets.Count; i++)
		{
			Rocket rocket = myFiredRockets[i];
			if ((bool)rocket)
			{
				ConfigNode configNode = new ConfigNode("firedRocket");
				eqNode.AddNode(configNode);
				configNode.SetValue("globalPos", VTMapManager.WorldToGlobalPoint(rocket.transform.position));
				configNode.SetValue("velocity", rocket.GetVelocity());
				configNode.SetValue("rotation", rocket.transform.rotation.eulerAngles);
				configNode.SetValue("elapsedTime", Time.time - rocket.GetTimeFired());
			}
		}
	}

	public override void OnQuickloadEquip(ConfigNode eqNode)
	{
		base.OnQuickloadEquip(eqNode);
		int count = ConfigNodeUtils.ParseInt(eqNode.GetValue("rocketCount"));
		LoadCount(count);
		foreach (ConfigNode node in eqNode.GetNodes("firedRocket"))
		{
			Vector3 position = VTMapManager.GlobalToWorldPoint(node.GetValue<Vector3D>("globalPos"));
			Vector3 value = node.GetValue<Vector3>("velocity");
			Vector3 value2 = node.GetValue<Vector3>("rotation");
			float value3 = node.GetValue<float>("elapsedTime");
			Rocket newRocketInstance = GetNewRocketInstance(null);
			newRocketInstance.transform.position = position;
			newRocketInstance.transform.rotation = Quaternion.Euler(value2);
			newRocketInstance.ResumeFire(parentActor, value, value3);
			myFiredRockets.Add(newRocketInstance);
			newRocketInstance.OnDetonated += OnRocketDetonated;
		}
	}
}
