using System;
using System.Collections;
using UnityEngine;

public class ObjectPowerUnit : ElectronicComponent, IQSVehicleComponent
{
	public GameObject objectToPower;

	public float drain = 1f;

	public bool connectedByDefault = true;

	public bool qsPersistent;

	private int _c = -1;

	private Coroutine cRoutine;

	private bool connected
	{
		get
		{
			return _c > 0;
		}
		set
		{
			int num = (value ? 1 : 0);
			if (_c == num)
			{
				return;
			}
			_c = num;
			connectedByDefault = value;
			if (value && base.gameObject.activeInHierarchy)
			{
				if (cRoutine != null)
				{
					StopCoroutine(cRoutine);
				}
				cRoutine = StartCoroutine(ConnectedRoutine());
			}
			this.OnPowerSwitched?.Invoke(num);
		}
	}

	public bool isConnected => connected;

	private string nodeName => "ObjectPowerUnit_" + base.gameObject.name;

	public event Action<int> OnPowerSwitched;

	private void Awake()
	{
		connected = connectedByDefault;
		if (!connected && (bool)objectToPower)
		{
			objectToPower.SetActive(value: false);
		}
	}

	public void SetConnection(int i)
	{
		if (i > 0)
		{
			connected = true;
		}
		else
		{
			connected = false;
		}
	}

	public void Disconnect()
	{
		connected = false;
	}

	public void Connect()
	{
		connected = true;
	}

	public void ToggleConnection()
	{
		connected = !connected;
	}

	private void OnEnable()
	{
		if (connected)
		{
			if (cRoutine != null)
			{
				StopCoroutine(cRoutine);
			}
			cRoutine = StartCoroutine(ConnectedRoutine());
		}
		else if ((bool)objectToPower)
		{
			objectToPower.SetActive(value: false);
		}
	}

	private IEnumerator ConnectedRoutine()
	{
		float time = 0.3f;
		WaitForSeconds wait = new WaitForSeconds(time);
		while (connected && (bool)objectToPower)
		{
			if (DrainElectricity(drain * time))
			{
				objectToPower.SetActive(value: true);
			}
			else
			{
				objectToPower.SetActive(value: false);
			}
			yield return wait;
		}
		if (!connected && (bool)objectToPower)
		{
			objectToPower.SetActive(value: false);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (qsPersistent)
		{
			qsNode.AddNode(nodeName).SetValue("connected", connected);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (qsPersistent)
		{
			ConfigNode node = qsNode.GetNode(nodeName);
			bool target = connectedByDefault;
			ConfigNodeUtils.TryParseValue(node, "connected", ref target);
			connected = target;
		}
	}
}
