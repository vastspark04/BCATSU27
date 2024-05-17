using UnityEngine;
using UnityEngine.UI;

public class VTTSaveHeightmapProgressWindow : MonoBehaviour
{
	public Text percentTxt;

	public Transform barTransform;

	public VTMapCustom.AsyncSaveOp op;

	private void OnEnable()
	{
		barTransform.localScale = new Vector3(0f, 1f, 1f);
		percentTxt.text = "0%";
	}

	private void Update()
	{
		if (op != null)
		{
			percentTxt.text = Mathf.Round(op.progress * 100f) + "%";
			barTransform.localScale = new Vector3(op.progress, 1f, 1f);
		}
	}
}
