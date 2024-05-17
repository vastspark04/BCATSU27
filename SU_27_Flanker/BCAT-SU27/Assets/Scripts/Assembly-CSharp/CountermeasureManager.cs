using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTNetworking;
using VTOLVR.Multiplayer;

public class CountermeasureManager : MonoBehaviour, IQSVehicleComponent, IPersistentVehicleData, ILocalizationUser
{
	public enum ReleaseModes
	{
		Single_Auto,
		Single_L,
		Single_R,
		Double
	}

	public delegate void CMEnabledDelegate(int cmIdx, bool _enabled);

	public Battery battery;

	public float drain = 1f;

	public MissileDetector launchDetector;

	public ModuleRWR rwr;

	public List<Countermeasure> countermeasures;

	public List<FlareCountermeasure> flareCMs;

	public List<ChaffCountermeasure> chaffCMs;

	public AudioSource announcementSource;

	public bool useCommonWarnings = true;

	public AudioClip flareAnnounceClip;

	public AudioClip chaffAnnounceClip;

	public float announcementMinInterval = 2f;

	private float timeChaffAnnounced;

	private float timeFlareAnnounced;

	private bool announcing;

	private string[] rModeLabels = new string[4] { "Single Auto", "Single L", "Single R", "Double" };

	private int rRateIdx;

	private int[] releaseRates = new int[7] { 0, 30, 60, 120, 240, 480, 960 };

	private bool powered;

	public FlightWarnings.FlightWarning lowCmWarning;

	private bool firedLowCMWarning;

	private bool firedLowChaffWarning;

	private bool firedEmptyChaffWarning;

	private bool firedLowFlareWarning;

	private bool firedEmptyFlareWarning;

	private FlightWarnings flightWarnings;

	private bool hasAppliedLoc;

	private string s_cms_rate_single;

	private bool firing;

	private string nodeName = "CountermeasureManager";

	public ReleaseModes releaseMode { get; private set; }

	public int releaseRateIdx => rRateIdx;

	public event Action<ReleaseModes> OnSetReleaseMode;

	public event Action OnReleaseRateChanged;

	public event UnityAction OnFiredCM;

	public event Action<int> OnFiredCMIdx;

	public event CMEnabledDelegate OnToggledCM;

	public void AnnounceFlare()
	{
		if ((bool)announcementSource && Time.time - timeFlareAnnounced > announcementMinInterval)
		{
			timeFlareAnnounced = Time.time;
			StartCoroutine(AnnounceRoutine(flareAnnounceClip));
		}
	}

	public void AnnounceChaff()
	{
		if ((bool)announcementSource && Time.time - timeChaffAnnounced > announcementMinInterval)
		{
			timeChaffAnnounced = Time.time;
			StartCoroutine(AnnounceRoutine(chaffAnnounceClip));
		}
	}

	private IEnumerator AnnounceRoutine(AudioClip clip)
	{
		if (!(clip == null))
		{
			while (announcing)
			{
				yield return null;
			}
			announcing = true;
			announcementSource.Stop();
			announcementSource.PlayOneShot(clip);
			yield return new WaitForSeconds(clip.length);
			announcing = false;
		}
	}

	public string GetReleaseModeLabel(ReleaseModes rMode)
	{
		if (!hasAppliedLoc)
		{
			ApplyLocalization();
			hasAppliedLoc = true;
		}
		return rModeLabels[(int)rMode];
	}

	public void SetReleaseMode(ReleaseModes r)
	{
		releaseMode = r;
		this.OnSetReleaseMode?.Invoke(r);
	}

	public string GetReleaseRateLabel()
	{
		if (!hasAppliedLoc)
		{
			ApplyLocalization();
			hasAppliedLoc = true;
		}
		if (rRateIdx == 0)
		{
			return s_cms_rate_single;
		}
		return releaseRates[rRateIdx].ToString();
	}

	[ContextMenu("Toggle Release Rates")]
	public void ToggleReleaseRates()
	{
		rRateIdx = (rRateIdx + 1) % releaseRates.Length;
		this.OnReleaseRateChanged?.Invoke();
	}

	public void IncreaseReleaseRate()
	{
		if (rRateIdx < releaseRates.Length - 1)
		{
			rRateIdx++;
		}
		this.OnReleaseRateChanged?.Invoke();
	}

	public void DecreaseReleaseRate()
	{
		if (rRateIdx > 0)
		{
			rRateIdx--;
		}
		this.OnReleaseRateChanged?.Invoke();
	}

	public void SetReleaseRateIdx(int r)
	{
		rRateIdx = r;
	}

	private void Awake()
	{
		if (flareCMs == null || flareCMs.Count == 0)
		{
			flareCMs = new List<FlareCountermeasure>();
			FlareCountermeasure[] componentsInChildren = GetComponentsInChildren<FlareCountermeasure>();
			foreach (FlareCountermeasure item in componentsInChildren)
			{
				flareCMs.Add(item);
			}
		}
		if (chaffCMs == null || chaffCMs.Count == 0)
		{
			chaffCMs = new List<ChaffCountermeasure>();
			ChaffCountermeasure[] componentsInChildren2 = GetComponentsInChildren<ChaffCountermeasure>();
			foreach (ChaffCountermeasure item2 in componentsInChildren2)
			{
				chaffCMs.Add(item2);
			}
		}
		foreach (Countermeasure countermeasure in countermeasures)
		{
			countermeasure.manager = this;
		}
		if (!hasAppliedLoc)
		{
			ApplyLocalization();
			hasAppliedLoc = true;
		}
	}

	public void ApplyLocalization()
	{
		s_cms_rate_single = VTLocalizationManager.GetString("s_cms_rate_single", "Single", "CMS release rate - single fire mode");
		for (int i = 0; i < rModeLabels.Length; i++)
		{
			rModeLabels[i] = VTLocalizationManager.GetString("s_cms_rMode_" + i, rModeLabels[i], "CMS release mode");
		}
	}

	private void Start()
	{
		flightWarnings = GetComponentInParent<FlightWarnings>();
		if ((bool)flightWarnings && useCommonWarnings)
		{
			flareAnnounceClip = flightWarnings.GetCommonWarningClip(FlightWarnings.CommonWarnings.Flare);
			chaffAnnounceClip = flightWarnings.GetCommonWarningClip(FlightWarnings.CommonWarnings.Chaff);
		}
	}

	private void OnEnable()
	{
		if ((bool)battery)
		{
			StartCoroutine(BatteryPoweredRoutine());
		}
	}

	private IEnumerator BatteryPoweredRoutine()
	{
		yield return null;
		float num = 0.1f;
		float f_drain = drain * num;
		WaitForSeconds wait = new WaitForSeconds(num);
		while (base.enabled)
		{
			if (battery.Drain(f_drain))
			{
				if (!powered)
				{
					for (int i = 0; i < countermeasures.Count; i++)
					{
						Countermeasure countermeasure = countermeasures[i];
						if ((bool)countermeasure.countText)
						{
							countermeasure.countText.enabled = true;
						}
					}
				}
				powered = true;
			}
			else
			{
				if (powered)
				{
					for (int j = 0; j < countermeasures.Count; j++)
					{
						Countermeasure countermeasure2 = countermeasures[j];
						if ((bool)countermeasure2.countText)
						{
							countermeasure2.countText.enabled = false;
						}
					}
				}
				powered = false;
			}
			yield return wait;
		}
	}

	public void FireCM()
	{
		if (rRateIdx == 0)
		{
			Internal_FireCM();
			return;
		}
		firing = true;
		StartCoroutine(RapidFireRoutine());
	}

	public void FireSingleCM()
	{
		Internal_FireCM();
	}

	private void Internal_FireCM()
	{
		if (!powered)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < countermeasures.Count; i++)
		{
			Countermeasure countermeasure = countermeasures[i];
			if (!countermeasure.enabled)
			{
				continue;
			}
			bool flag2 = countermeasure.FireCM();
			flag = flag || flag2;
			bool flag3 = i == 0;
			if (flag2)
			{
				this.OnFiredCMIdx?.Invoke(i);
				if ((bool)flightWarnings)
				{
					bool flag4 = firedLowCMWarning;
					if (useCommonWarnings)
					{
						flag4 = (flag3 ? firedLowChaffWarning : firedLowFlareWarning);
					}
					if (!flag4 && (float)countermeasure.count / (float)countermeasure.maxCount < 0.15f)
					{
						if (useCommonWarnings)
						{
							if (flag3)
							{
								flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.ChaffLow);
								firedLowChaffWarning = true;
							}
							else
							{
								flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.FlareLow);
								firedLowFlareWarning = true;
							}
						}
						else if (lowCmWarning != null)
						{
							flightWarnings.AddOneShotWarning(lowCmWarning);
						}
						firedLowCMWarning = true;
					}
					if ((float)countermeasure.count / (float)countermeasure.maxCount > 0.16f)
					{
						firedLowCMWarning = false;
						if (flag3)
						{
							firedLowChaffWarning = false;
							firedEmptyChaffWarning = false;
						}
						else
						{
							firedLowFlareWarning = false;
							firedEmptyFlareWarning = false;
						}
					}
				}
				if (flag3)
				{
					AnnounceChaff();
				}
				else
				{
					AnnounceFlare();
				}
			}
			else
			{
				if (!flightWarnings)
				{
					continue;
				}
				if (flag3)
				{
					if (!firedEmptyChaffWarning)
					{
						flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.ChaffEmpty);
						firedEmptyChaffWarning = true;
					}
				}
				else if (!firedEmptyFlareWarning)
				{
					flightWarnings.AddCommonWarning(FlightWarnings.CommonWarnings.FlareEmpty);
					firedEmptyFlareWarning = true;
				}
			}
		}
		if (flag && this.OnFiredCM != null)
		{
			this.OnFiredCM();
		}
	}

	private IEnumerator RapidFireRoutine()
	{
		float interval = 60f / (float)releaseRates[rRateIdx];
		while (firing && rRateIdx != 0)
		{
			Internal_FireCM();
			float t = Time.time;
			while (firing && Time.time - t < interval)
			{
				yield return null;
			}
		}
	}

	public void StopFireCM()
	{
		firing = false;
	}

	public void ToggleCM(int idx)
	{
		if (countermeasures != null && idx >= 0 && idx < countermeasures.Count)
		{
			countermeasures[idx].enabled = !countermeasures[idx].enabled;
			this.OnToggledCM?.Invoke(idx, countermeasures[idx].enabled);
		}
	}

	public void SetFlare(int st)
	{
		foreach (FlareCountermeasure flareCM in flareCMs)
		{
			flareCM.enabled = st > 0;
		}
		this.OnToggledCM?.Invoke(1, st > 0);
	}

	public void SetChaff(int st)
	{
		foreach (ChaffCountermeasure chaffCM in chaffCMs)
		{
			chaffCM.enabled = st > 0;
		}
		this.OnToggledCM?.Invoke(0, st > 0);
	}

	public void SetCM(int cmIdx, int st)
	{
		countermeasures[cmIdx].enabled = st > 0;
		this.OnToggledCM?.Invoke(cmIdx, st > 0);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		configNode.SetValue("rRateIdx", rRateIdx);
		configNode.SetValue("releaseMode", releaseMode);
		configNode.SetValue("chaffEnabled", countermeasures[0].enabled);
		configNode.SetValue("flaresEnabled", countermeasures[1].enabled);
		for (int i = 0; i < flareCMs.Count; i++)
		{
			ConfigNode configNode2 = new ConfigNode("FLARES");
			configNode2.SetValue("idx", i.ToString());
			configNode2.SetValue("l_count", flareCMs[i].leftCount);
			configNode2.SetValue("r_count", flareCMs[i].rightCount);
			configNode.AddNode(configNode2);
		}
		for (int j = 0; j < chaffCMs.Count; j++)
		{
			ConfigNode configNode3 = new ConfigNode("CHAFF");
			configNode3.SetValue("idx", j.ToString());
			configNode3.SetValue("l_count", chaffCMs[j].leftCount);
			configNode3.SetValue("r_count", chaffCMs[j].rightCount);
			configNode.AddNode(configNode3);
		}
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (!qsNode.HasNode(nodeName))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(nodeName);
		ConfigNodeUtils.TryParseValue(node, "rRateIdx", ref rRateIdx);
		ReleaseModes target = releaseMode;
		ConfigNodeUtils.TryParseValue(node, "releaseMode", ref target);
		SetReleaseMode(target);
		countermeasures[0].enabled = node.GetValue<bool>("chaffEnabled");
		countermeasures[1].enabled = node.GetValue<bool>("flaresEnabled");
		foreach (ConfigNode node2 in node.GetNodes("FLARES"))
		{
			int index = ConfigNodeUtils.ParseInt(node2.GetValue("idx"));
			int leftCount = ConfigNodeUtils.ParseInt(node2.GetValue("l_count"));
			int rightCount = ConfigNodeUtils.ParseInt(node2.GetValue("r_count"));
			flareCMs[index].leftCount = leftCount;
			flareCMs[index].rightCount = rightCount;
			flareCMs[index].UpdateCountText();
		}
		foreach (ConfigNode node3 in node.GetNodes("CHAFF"))
		{
			int index2 = ConfigNodeUtils.ParseInt(node3.GetValue("idx"));
			int leftCount2 = ConfigNodeUtils.ParseInt(node3.GetValue("l_count"));
			int rightCount2 = ConfigNodeUtils.ParseInt(node3.GetValue("r_count"));
			chaffCMs[index2].leftCount = leftCount2;
			chaffCMs[index2].rightCount = rightCount2;
			chaffCMs[index2].UpdateCountText();
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		ConfigNode configNode = vDataNode.AddOrGetNode(nodeName);
		configNode.SetValue("rRateIdx", rRateIdx);
		configNode.SetValue("releaseMode", releaseMode);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			VTNetEntity component = GetComponent<VTNetEntity>();
			if ((bool)component && !component.isMine)
			{
				return;
			}
		}
		ConfigNode node = vDataNode.GetNode(nodeName);
		if (node != null)
		{
			ConfigNodeUtils.TryParseValue(node, "rRateIdx", ref rRateIdx);
			ReleaseModes target = releaseMode;
			ConfigNodeUtils.TryParseValue(node, "releaseMode", ref target);
			releaseMode = target;
		}
	}
}
