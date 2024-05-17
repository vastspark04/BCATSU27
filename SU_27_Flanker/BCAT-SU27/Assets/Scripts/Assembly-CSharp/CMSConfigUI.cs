using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class CMSConfigUI : MonoBehaviour
{
	public CountermeasureManager cmm;

	public MultiUserVehicleSync muvs;

	[Header("Chaff")]
	public Text chaffCount;

	public Transform chaffBar;

	public Transform chaffBarR;

	public GameObject chaffIndicator;

	[Header("Flares")]
	public Text flareCount;

	public Transform flareBar;

	public Transform flareBarR;

	public GameObject flareIndicator;

	[Header("Release")]
	public Text releaseModeValueText;

	public Text releaseRateValueText;

	private void Start()
	{
		cmm.OnFiredCM += UpdateCounts;
		cmm.OnSetReleaseMode += Cmm_OnSetReleaseMode;
		if ((bool)muvs)
		{
			muvs.OnCMCountsUpdated += UpdateCounts;
			muvs.OnReleaseRateChanged += UpdateRateText;
		}
		UpdateCounts();
		UpdateRModeText();
		releaseRateValueText.text = cmm.GetReleaseRateLabel();
		if ((bool)chaffIndicator)
		{
			chaffIndicator.SetActive(cmm.countermeasures[0].enabled);
		}
		if ((bool)flareIndicator)
		{
			flareIndicator.SetActive(cmm.countermeasures[1].enabled);
		}
		GetComponentInParent<PlayerVehicleSetup>().OnEndUsingConfigurator += CMSConfigUI_OnEndUsingConfigurator;
		cmm.OnToggledCM += Cmm_OnToggledCM;
		Cmm_OnToggledCM(0, cmm.countermeasures[0].enabled);
	}

	private void Cmm_OnToggledCM(int cmIdx, bool _enabled)
	{
		if ((bool)chaffIndicator)
		{
			chaffIndicator.SetActive(cmm.countermeasures[0].enabled);
		}
		if ((bool)flareIndicator)
		{
			flareIndicator.SetActive(cmm.countermeasures[1].enabled);
		}
	}

	private void Cmm_OnSetReleaseMode(CountermeasureManager.ReleaseModes obj)
	{
		UpdateRModeText();
	}

	private void CMSConfigUI_OnEndUsingConfigurator(LoadoutConfigurator arg0)
	{
		UpdateCounts();
	}

	private void UpdateCounts()
	{
		int num = cmm.chaffCMs[0].count;
		int num2 = cmm.flareCMs[0].count;
		int num3 = cmm.chaffCMs[0].leftCount;
		int num4 = cmm.chaffCMs[0].rightCount;
		int num5 = cmm.flareCMs[0].leftCount;
		int num6 = cmm.flareCMs[0].rightCount;
		if ((bool)muvs && muvs.wasRegistered && !muvs.isMine)
		{
			num = muvs.chaffCountL + muvs.chaffCountR;
			num2 = muvs.flareCountL + muvs.flareCountR;
			num3 = muvs.chaffCountL;
			num4 = muvs.chaffCountR;
			num5 = muvs.flareCountL;
			num6 = muvs.flareCountR;
		}
		chaffCount.text = num.ToString();
		flareCount.text = num2.ToString();
		if ((bool)chaffBar)
		{
			if ((bool)chaffBarR)
			{
				chaffBar.localScale = new Vector3((float)num3 / (float)(cmm.chaffCMs[0].maxCount / 2), 1f, 1f);
				chaffBarR.localScale = new Vector3((float)num4 / (float)(cmm.chaffCMs[0].maxCount / 2), 1f, 1f);
			}
			else
			{
				chaffBar.localScale = new Vector3((float)num / (float)cmm.chaffCMs[0].maxCount, 1f, 1f);
			}
		}
		if ((bool)flareBar)
		{
			if ((bool)flareBarR)
			{
				flareBar.localScale = new Vector3((float)num5 / (float)(cmm.flareCMs[0].maxCount / 2), 1f, 1f);
				flareBarR.localScale = new Vector3((float)num6 / (float)(cmm.flareCMs[0].maxCount / 2), 1f, 1f);
			}
			else
			{
				flareBar.localScale = new Vector3((float)num2 / (float)cmm.flareCMs[0].maxCount, 1f, 1f);
			}
		}
	}

	public void SetChaff(int state)
	{
		cmm.SetChaff(state);
	}

	public void ToggleChaff()
	{
		SetChaff((!cmm.countermeasures[0].enabled) ? 1 : 0);
	}

	public void SetFlares(int state)
	{
		cmm.SetFlare(state);
	}

	public void ToggleFlares()
	{
		SetFlares((!cmm.countermeasures[1].enabled) ? 1 : 0);
	}

	public void ToggleReleaseModeButton()
	{
		switch (cmm.releaseMode)
		{
		case CountermeasureManager.ReleaseModes.Single_Auto:
			cmm.SetReleaseMode(CountermeasureManager.ReleaseModes.Single_L);
			break;
		case CountermeasureManager.ReleaseModes.Single_L:
			cmm.SetReleaseMode(CountermeasureManager.ReleaseModes.Single_R);
			break;
		case CountermeasureManager.ReleaseModes.Single_R:
			cmm.SetReleaseMode(CountermeasureManager.ReleaseModes.Double);
			break;
		case CountermeasureManager.ReleaseModes.Double:
			cmm.SetReleaseMode(CountermeasureManager.ReleaseModes.Single_Auto);
			break;
		}
		UpdateRModeText();
	}

	private void UpdateRModeText()
	{
		releaseModeValueText.text = cmm.GetReleaseModeLabel(cmm.releaseMode);
	}

	public void IncreaseReleaseRateButton()
	{
		cmm.IncreaseReleaseRate();
		UpdateRateText();
	}

	public void DecreaseReleaseRateButton()
	{
		cmm.DecreaseReleaseRate();
		UpdateRateText();
	}

	private void UpdateRateText()
	{
		releaseRateValueText.text = cmm.GetReleaseRateLabel();
	}
}
