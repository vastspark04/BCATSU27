using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class CMSCountSync : VTNetSyncRPCOnly
{
	public ChaffCountermeasure chaff;

	public FlareCountermeasure flare;

	public Text chaffText;

	public Text flareText;

	public MultiUserVehicleSync muvs;

	private bool hasSetupLocal;

	protected override void Awake()
	{
	}

	private void Start()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			SetupLocal();
		}
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (base.isMine)
		{
			SetupLocal();
			return;
		}
		muvs.OnCMCountsUpdated += Muvs_OnCMCountsUpdated;
		Muvs_OnCMCountsUpdated();
	}

	private void Muvs_OnCMCountsUpdated()
	{
		chaffText.text = muvs.chaffCountTotal.ToString();
		flareText.text = muvs.flareCountTotal.ToString();
	}

	private void SetupLocal()
	{
		if (!hasSetupLocal)
		{
			hasSetupLocal = true;
			chaff.OnCountUpdated += Chaff_OnCountUpdated;
			flare.OnCountUpdated += Flare_OnCountUpdated;
			chaffText.text = chaff.count.ToString();
			flareText.text = flare.count.ToString();
		}
	}

	private void Flare_OnCountUpdated(int count)
	{
		flareText.text = count.ToString();
	}

	private void Chaff_OnCountUpdated(int count)
	{
		chaffText.text = count.ToString();
	}
}

}