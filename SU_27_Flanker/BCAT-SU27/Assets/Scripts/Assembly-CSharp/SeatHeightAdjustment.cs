using UnityEngine;

public class SeatHeightAdjustment : MonoBehaviour, IPersistentVehicleData
{
	public float minHeight;

	public float maxHeight;

	public Transform[] seatTransforms;

	public float adjustRate;

	private float normHeight;

	private bool loadedHeight;

	private string nodeName => base.gameObject.name + "_SeatHeightAdjustment";

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNode node = vDataNode.GetNode(nodeName);
		if (node != null)
		{
			ConfigNodeUtils.TryParseValue(node, "normHeight", ref normHeight);
			UpdateHeight();
			loadedHeight = true;
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		vDataNode.AddOrGetNode(nodeName).SetValue("normHeight", normHeight);
	}

	private void UpdateHeight()
	{
		float y = Mathf.Lerp(minHeight, maxHeight, normHeight);
		for (int i = 0; i < seatTransforms.Length; i++)
		{
			Transform transform = seatTransforms[i];
			if ((bool)transform)
			{
				transform.transform.localPosition = new Vector3(0f, y, 0f);
			}
		}
	}

	private void Start()
	{
		if (!loadedHeight)
		{
			normHeight = Mathf.InverseLerp(minHeight, maxHeight, 0f);
			UpdateHeight();
		}
	}

	public void IncreaseHeight()
	{
		float num = adjustRate * Time.deltaTime * (maxHeight - minHeight);
		normHeight = Mathf.Clamp01(normHeight + num);
		UpdateHeight();
	}

	public void DecreaseHeight()
	{
		float num = adjustRate * Time.deltaTime * (maxHeight - minHeight);
		normHeight = Mathf.Clamp01(normHeight - num);
		UpdateHeight();
	}
}
