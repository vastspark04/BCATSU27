using UnityEngine;

[ExecuteInEditMode]
public class TC_AnimateTransform : MonoBehaviour
{
	public bool animate = true;

	public float rotSpeed;

	public Vector3 moveSpeed;

	public float scaleSpeed;

	public float scaleAmplitude;

	public float scaleOffset;

	private Vector3 posOld;

	private float time;

	private void Update()
	{
		MyUpdate();
	}

	private void MyUpdate()
	{
		if (animate)
		{
			float num = Time.realtimeSinceStartup - time;
			base.transform.Rotate(0f, rotSpeed * num, 0f);
			base.transform.Translate(moveSpeed * num * 90f);
			float num2 = Mathf.Sin(Time.realtimeSinceStartup * scaleSpeed) * scaleAmplitude + scaleOffset;
			base.transform.localScale = new Vector3(num2, num2, num2);
			time = Time.realtimeSinceStartup;
		}
	}

	private void OnDrawGizmos()
	{
		Event current = Event.current;
		if (current.shift && current.type == EventType.KeyUp)
		{
			animate = !animate;
		}
	}
}
