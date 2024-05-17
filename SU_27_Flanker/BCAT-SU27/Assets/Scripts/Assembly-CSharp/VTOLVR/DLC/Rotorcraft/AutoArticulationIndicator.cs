using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class AutoArticulationIndicator : MonoBehaviour
{
	public ArticulatingHardpoint aHp;

	public GameObject indicatorObj;

	private void Update()
	{
		indicatorObj.SetActive(aHp.autoMode);
	}
}

}