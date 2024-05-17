using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class RotorFlapAnimator : MonoBehaviour
{
	public HeliWingFlapper[] flappers;

	public Transform[] animBladeTransforms;

	private void Update()
	{
		for (int i = 0; i < animBladeTransforms.Length; i++)
		{
			float num = 0f;
			float num2 = 0f;
			for (int j = 0; j < flappers.Length; j++)
			{
				float num3 = Vector3.Dot(flappers[j].transform.forward, animBladeTransforms[i].parent.forward);
				if (num3 > 0f)
				{
					num += num3 * flappers[j].currentFlap;
					num2 += num3;
				}
			}
			num /= num2;
			animBladeTransforms[i].localEulerAngles = new Vector3(0f - num, 0f, 0f);
		}
	}
}

}