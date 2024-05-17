using UnityEngine;

public class AirBrakeController : MonoBehaviour
{
	private float brake;

	public SimpleDrag brakeDrag;

	public float brakeDragArea = 0.08f;

	public float brakeDragRate = 0.045f;

	private void Update()
	{
		if ((bool)brakeDrag)
		{
			brakeDrag.area = Mathf.MoveTowards(brakeDrag.area, brakeDragArea * brake, brakeDragRate * Time.deltaTime);
		}
	}

	public void SetBrake(float t)
	{
		brake = t;
	}
}
