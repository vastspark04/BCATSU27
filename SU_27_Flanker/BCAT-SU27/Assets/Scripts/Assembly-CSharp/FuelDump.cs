using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class FuelDump : MonoBehaviour, IQSVehicleComponent
{
	public FuelTank fuelTank;

	public EmissiveTextureLight indicatorLight;

	public AudioSource indicatorAudio;

	public AudioSource dumpAudio;

	public ParticleSystem[] particleSystems;

	public Battery battery;

	private FlightWarnings fw;

	public float dumpRate;

	private bool dumping;

	private bool remote;

	private Coroutine dumpRoutine;

	private bool alive = true;

	public UnityEvent OnDumpWhileBroken;

	public bool isDumping => dumping;

	public event Action<int> OnDumpState;

	private void Awake()
	{
		if ((bool)dumpAudio)
		{
			dumpAudio.Stop();
			dumpAudio.volume = 0f;
		}
		if (particleSystems != null)
		{
			particleSystems.SetEmission(emit: false);
		}
		fw = GetComponentInParent<FlightWarnings>();
		if (!battery)
		{
			battery = base.transform.root.GetComponentInChildren<Battery>();
		}
	}

	public void SetToRemote()
	{
		remote = true;
	}

	public void SetDump(int state)
	{
		dumping = state > 0;
		if (dumping)
		{
			if ((bool)fw)
			{
				fw.AddCommonWarningContinuous(FlightWarnings.CommonWarnings.FuelDump);
			}
			if (dumpRoutine != null)
			{
				StopCoroutine(dumpRoutine);
			}
			dumpRoutine = StartCoroutine(DumpRoutine());
		}
		else
		{
			if ((bool)fw)
			{
				fw.RemoveCommonWarning(FlightWarnings.CommonWarnings.FuelDump);
			}
			if (dumpRoutine != null)
			{
				StopCoroutine(dumpRoutine);
			}
			if ((bool)indicatorLight)
			{
				indicatorLight.SetStatus(0);
			}
			if (particleSystems != null)
			{
				particleSystems.SetEmission(emit: false);
			}
			if (indicatorAudio != null)
			{
				indicatorAudio.Stop();
			}
			if (dumpAudio != null)
			{
				dumpAudio.Stop();
				dumpAudio.volume = 0f;
			}
		}
		this.OnDumpState?.Invoke(state);
	}

	private IEnumerator DumpRoutine()
	{
		WaitForFixedUpdate wait = new WaitForFixedUpdate();
		float startTime = Time.time;
		int lit = 1;
		while (true)
		{
			if (!remote && (bool)battery && !battery.Drain(0.01f * Time.fixedDeltaTime))
			{
				SetDump(0);
				break;
			}
			if (!alive)
			{
				OnDumpWhileBroken?.Invoke();
				SetDump(0);
				break;
			}
			if (!remote && !(fuelTank.RequestFuel(dumpRate * Time.fixedDeltaTime) > 0f))
			{
				break;
			}
			if ((bool)indicatorLight)
			{
				int num = ((Mathf.Repeat((Time.time - startTime) * 2f, 1f) < 0.75f) ? 1 : 0);
				if (num != lit)
				{
					lit = num;
					indicatorLight.SetStatus(lit);
				}
			}
			if ((bool)dumpAudio)
			{
				dumpAudio.volume = Mathf.Lerp(dumpAudio.volume, 1f, 5f * Time.deltaTime);
			}
			if (particleSystems != null)
			{
				particleSystems.SetEmission(emit: true);
			}
			if (indicatorAudio != null && !indicatorAudio.isPlaying)
			{
				indicatorAudio.Play();
			}
			if (dumpAudio != null && !dumpAudio.isPlaying)
			{
				dumpAudio.Play();
			}
			yield return wait;
		}
		if (dumpAudio != null)
		{
			dumpAudio.Stop();
			dumpAudio.volume = 0f;
		}
		if (particleSystems != null)
		{
			particleSystems.SetEmission(emit: false);
		}
		if ((bool)indicatorLight)
		{
			indicatorLight.SetStatus(1);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("FuelDump");
		configNode.SetValue("dumping", dumping);
		configNode.SetValue("alive", alive);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("FuelDump");
		if (node != null)
		{
			if (node.GetValue<bool>("dumping"))
			{
				SetDump(1);
			}
			ConfigNodeUtils.TryParseValue(node, "alive", ref alive);
		}
	}

	public void DestroyDumpFunction()
	{
		alive = false;
	}

	public void RepairDumpFunction()
	{
		alive = true;
	}
}
