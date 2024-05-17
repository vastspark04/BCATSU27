using UnityEngine;
using UnityEngine.UI;

public class MFDPRadarWarningReceiver : MFDPortalPage
{
	public DashRWR dashRwr;

	public float iconScale;

	public float subIconScale;

	public AspectRatioFitter uiFitter;

	protected override void OnSetPageState(PageStates s)
	{
		base.OnSetPageState(s);
		uiFitter.SetLayoutHorizontal();
		uiFitter.SetLayoutVertical();
		dashRwr.UpdateIconsForUISize((s == PageStates.SubSized) ? subIconScale : iconScale);
		dashRwr.iconWidth = ((RectTransform)dashRwr.radarIconTemplate.transform).rect.width * iconScale;
	}
}
