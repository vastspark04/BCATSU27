using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CatapultHook : MonoBehaviour
{
	public ModuleTurret hookTurret;

	public Transform hookForcePointTransform;

	public Transform hookExtensionTransform;

	public Transform preHookAimTarget;

	public Rigidbody rb;

	private CarrierCatapult _catapult;

	public GearAnimator gearAnimator;

	public bool startDeployed;

	public ModuleEngine[] engines;

	public UnityEvent OnHooked;

	public bool deployed { get; private set; }

	public bool hooked { get; private set; }

	public CarrierCatapult catapult => _catapult;

	public bool remote { get; private set; }

	public event Action<int> OnExtendState;

	public void SetToRemote()
	{
		remote = true;
	}

	[ContextMenu("Get Engines In Children")]
	public void GetEnginesInChildren()
	{
		engines = rb.GetComponentsInChildren<ModuleEngine>();
	}

	private void Start()
	{
		deployed = false;
		if (startDeployed)
		{
			Toggle();
		}
	}

	public void Toggle()
	{
		deployed = !deployed;
		if (!deployed)
		{
			if (hooked)
			{
				StartCoroutine(RetractOnRelease());
			}
			else
			{
				hooked = false;
				_catapult = null;
				if ((bool)hookTurret)
				{
					hookTurret.ReturnTurretOneshot();
				}
				if ((bool)hookExtensionTransform)
				{
					hookExtensionTransform.localPosition = Vector3.zero;
				}
			}
		}
		else
		{
			StartCoroutine(DeployedRoutine());
		}
		this.OnExtendState?.Invoke(deployed ? 1 : 0);
	}

	private IEnumerator RetractOnRelease()
	{
		while (hooked && !deployed)
		{
			yield return null;
		}
		if (!deployed)
		{
			if ((bool)hookTurret)
			{
				hookTurret.ReturnTurretOneshot();
			}
			if ((bool)hookExtensionTransform)
			{
				hookExtensionTransform.localPosition = Vector3.zero;
			}
		}
	}

	public void Retract()
	{
		if (deployed)
		{
			Toggle();
		}
	}

	public void Extend()
	{
		if (!deployed)
		{
			Toggle();
		}
	}

	public void SetState(int state)
	{
		if (state == 1)
		{
			Extend();
		}
		else
		{
			Retract();
		}
	}

	public void RemoteHook(CarrierCatapult c)
	{
		hooked = true;
		c.Hook(this);
		_catapult = c;
		OnHooked?.Invoke();
	}

	private IEnumerator DeployedRoutine()
	{
		while (hooked || (deployed && !hooked))
		{
			if (!hooked)
			{
				bool flag = true;
				if ((bool)gearAnimator && gearAnimator.GetCurrentState() != 0)
				{
					flag = false;
				}
				if (flag && !remote)
				{
					hooked = CarrierCatapultManager.CheckForCatapult(this, out _catapult);
					if (hooked && OnHooked != null)
					{
						OnHooked.Invoke();
					}
				}
				if ((bool)hookTurret)
				{
					if (!flag)
					{
						hookTurret.ReturnTurret();
					}
					else
					{
						hookTurret.AimToTarget(preHookAimTarget.position);
					}
				}
			}
			else
			{
				UpdateHooked();
			}
			yield return null;
		}
	}

	private void UpdateHooked()
	{
		if (catapult.hooked)
		{
			if ((bool)hookTurret)
			{
				hookTurret.AimToTarget(catapult.catapultTransform.position);
			}
			if ((bool)hookExtensionTransform)
			{
				Vector3 zero = Vector3.zero;
				zero.z = Vector3.Dot(hookExtensionTransform.forward, catapult.catapultTransform.position - hookExtensionTransform.parent.position);
				zero.z = Mathf.Max(zero.z, 0f);
				hookExtensionTransform.localPosition = zero;
			}
		}
		else
		{
			hooked = false;
			if ((bool)hookExtensionTransform)
			{
				hookExtensionTransform.localPosition = Vector3.zero;
			}
		}
	}
}
