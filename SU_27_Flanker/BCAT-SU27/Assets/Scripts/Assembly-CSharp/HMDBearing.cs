using UnityEngine;
using UnityEngine.UI;

public class HMDBearing : MonoBehaviour
{
	public Transform referenceTransform;

	public Text bearingText;

	public Transform ladderTransform;

	public float zeroBearing = 60f;

	public float degreesToPixels = 26.2f;

	private void Update()
	{
		Vector3 forward = referenceTransform.forward;
		forward.y = 0f;
		float num = VectorUtils.SignedAngle(Vector3.forward, forward, Vector3.right);
		if (num < 0f)
		{
			num += 360f;
		}
		Vector3 localPosition = ladderTransform.localPosition;
		float num2 = (localPosition.x = (0f - (num - zeroBearing)) * degreesToPixels);
		ladderTransform.localPosition = localPosition;
		bearingText.text = Mathf.Round(num).ToString();
	}
}
