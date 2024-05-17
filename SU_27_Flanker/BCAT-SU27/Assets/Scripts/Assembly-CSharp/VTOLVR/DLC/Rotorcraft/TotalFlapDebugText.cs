using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.DLC.Rotorcraft{

public class TotalFlapDebugText : MonoBehaviour
{
	public RotorFlapCalculator flapCalc;

	public Text text;

	private void Update()
	{
		text.text = flapCalc.totalFlap.ToString();
	}
}

}