using System.IO;
using UnityEngine;

public class TestCampaignRepair : MonoBehaviour
{
	public string campaignName;

	[ContextMenu("Repair")]
	public void Repair()
	{
		string campaignDir = Path.Combine(VTResources.customCampaignsDir, campaignName);
		bool flag = VTCampaignInfo.RepairCampaignFileStructure(null, campaignDir);
		Debug.LogFormat("The campaign was {0}repaired", flag ? "" : "NOT ");
	}
}
