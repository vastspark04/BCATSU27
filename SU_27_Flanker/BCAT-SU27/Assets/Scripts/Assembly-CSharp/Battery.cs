using UnityEngine;

public class Battery : MonoBehaviour, IQSVehicleComponent
{
	public float startingCharge;

	public float maxCharge;

	public bool connectedByDefault = true;

	private bool isAlive = true;

	private bool isRemote;

	public float currentCharge { get; private set; }

	public bool connected { get; private set; }

	private void Awake()
	{
		currentCharge = Mathf.Clamp(startingCharge, 0f, maxCharge);
		connected = connectedByDefault;
		Health componentInParent = GetComponentInParent<Health>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDeath.AddListener(Kill);
		}
		VehiclePart componentInParent2 = GetComponentInParent<VehiclePart>();
		if ((bool)componentInParent2)
		{
			componentInParent2.OnRepair.AddListener(OnRepair);
		}
	}

	private void OnRepair()
	{
		if (!isAlive)
		{
			isAlive = true;
			Debug.Log("Repairing Battery.");
		}
	}

	public void ToggleConnection()
	{
		connected = !connected;
	}

	public void SetConnection(int c)
	{
		if (c > 0)
		{
			Connect();
		}
		else
		{
			Disconnect();
		}
	}

	public void SetConnection3Way(int c)
	{
		switch (c)
		{
		case 2:
			Connect();
			break;
		case 0:
			Disconnect();
			break;
		}
	}

	public void Connect()
	{
		connected = true;
	}

	public void Disconnect()
	{
		connected = false;
	}

	public bool Drain(float drainAmount)
	{
		if (connected && (isRemote || (isAlive && currentCharge >= drainAmount)))
		{
			currentCharge -= drainAmount;
			currentCharge = Mathf.Max(0f, currentCharge);
			return true;
		}
		return false;
	}

	public void Charge(float chargeAmount)
	{
		if (connected && isAlive)
		{
			currentCharge = Mathf.Min(currentCharge + chargeAmount, maxCharge);
		}
	}

	public void Kill()
	{
		isAlive = false;
	}

	public void SetToRemote()
	{
		isRemote = true;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_Battery");
		configNode.SetValue("currentCharge", currentCharge);
		configNode.SetValue("connected", connected);
		configNode.SetValue("isAlive", isAlive);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_Battery";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			currentCharge = ConfigNodeUtils.ParseFloat(node.GetValue("currentCharge"));
			connected = ConfigNodeUtils.ParseBool(node.GetValue("connected"));
			isAlive = ConfigNodeUtils.ParseBool(node.GetValue("isAlive"));
		}
	}
}
