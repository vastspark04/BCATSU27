using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace VTNetworking{

public class VTNetAnalyticsUI : MonoBehaviour
{
	private delegate void UpdateTextDelegate(Text valueText, int newValue);

	private const int HISTORY_LENGTH = 100;

	public float graphVerticalScale = 100f;

	public float graphHorizontalScale = 5f;

	[Header("Upstream")]
	public UILineRenderer upstreamGraphLR;

	public Text upstreamValueText;

	private int[] upstreamHistory = new int[100];

	private int upstreamIdx;

	private Vector2[] upstreamPoints = new Vector2[100];

	[Header("Downstream")]
	public UILineRenderer downstreamGraphLR;

	public Text downstreamValueText;

	private int[] downstreamHistory = new int[100];

	private int downstreamIdx;

	private Vector2[] downstreamPoints = new Vector2[100];

	[Header("Framerate")]
	public bool doFramerateGraph;

	public UILineRenderer framerateGraphLR;

	public Text framerateValueText;

	public float fpsVerticalScale = 0.75f;

	private int[] framerateHistory = new int[100];

	private int framerateIdx;

	private Vector2[] frameratePoints = new Vector2[100];

	public Text sendMessageResultText;

	private const int FRAME_AVG_COUNT = 20;

	private float[] frameTimes = new float[20];

	private int frameRecordIdx;

	public float maxY = 50f;

	private void OnEnable()
	{
		if ((bool)VTNetworkManager.instance)
		{
			VTNetworkManager.instance.OnUpdatedAnalytics += UpdateGraphs;
		}
		if (doFramerateGraph)
		{
			StartCoroutine(FramerateGraphRoutine());
		}
	}

	private IEnumerator FramerateGraphRoutine()
	{
		while (base.enabled)
		{
			frameTimes[frameRecordIdx] = Time.deltaTime;
			frameRecordIdx = (frameRecordIdx + 1) % 20;
			if (frameRecordIdx == 0)
			{
				float num = 0f;
				for (int i = 0; i < 20; i++)
				{
					num += frameTimes[i];
				}
				num /= 20f;
				int newValue = Mathf.RoundToInt(1f / num);
				UpdateGraph(framerateGraphLR, framerateValueText, framerateHistory, ref framerateIdx, frameratePoints, newValue, fpsVerticalScale, UpdateTextFPS);
			}
			yield return null;
		}
	}

	private void OnDisable()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnUpdatedAnalytics -= UpdateGraphs;
		}
	}

	private void UpdateGraphs()
	{
		UpdateGraph(upstreamGraphLR, upstreamValueText, upstreamHistory, ref upstreamIdx, upstreamPoints, VTNetworkManager.instance.upstreamRate, graphVerticalScale, UpdateTextData);
		UpdateGraph(downstreamGraphLR, downstreamValueText, downstreamHistory, ref downstreamIdx, downstreamPoints, VTNetworkManager.instance.downstreamRate, graphVerticalScale, UpdateTextData);
		if ((bool)sendMessageResultText)
		{
			if (VTNetworkManager.isHost)
			{
				sendMessageResultText.text = "Host";
			}
			else
			{
				sendMessageResultText.text = "Client " + VTNetworkManager.instance.clientSendMessageResult;
			}
		}
	}

	private void UpdateGraph(UILineRenderer lr, Text valueText, int[] history, ref int idx, Vector2[] points, int newValue, float vertScale, UpdateTextDelegate txtUpdate)
	{
		history[idx] = newValue;
		txtUpdate(valueText, newValue);
		idx = (idx + 1) % 100;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < 100; i++)
		{
			float num3 = history[i];
			num2 += num3;
			num = Mathf.Max(num, num3);
		}
		float num4 = Mathf.Max(vertScale, num / maxY);
		int num5 = 0;
		int num6 = idx;
		while (num5 < 100)
		{
			float y = (float)history[num6] / num4;
			Vector2 vector = (points[num5] = new Vector2((float)num5 * graphHorizontalScale, y));
			num5++;
			num6 = (num6 + 1) % 100;
		}
		lr.Points = points;
		lr.SetVerticesDirty();
	}

	private void UpdateTextData(Text valueText, int newValue)
	{
		float num = (float)newValue / 1000f;
		valueText.text = string.Format(arg1: (num * 8f / 1000f).ToString("0.000"), format: "{0} kb/s, ({1} mbit/s)", arg0: num.ToString("0.00"));
	}

	private void UpdateTextFPS(Text valueText, int newValue)
	{
		valueText.text = newValue.ToString();
	}
}

}