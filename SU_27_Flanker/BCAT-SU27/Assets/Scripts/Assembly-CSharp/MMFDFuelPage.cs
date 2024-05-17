using UnityEngine;
using UnityEngine.UI;

public class MMFDFuelPage : MonoBehaviour
{
	public MFDPage mfdPage;

	public FuelTank fuelTank;

	public Transform mainFuelTf;

	public Text mainFuelText;

	public Transform extFuelTf;

	public Text extFuelText;

	private void Update()
	{
		if (!mfdPage || mfdPage.isOpen)
		{
			mainFuelTf.localScale = new Vector3(fuelTank.fuelFraction, 1f, 1f);
			mainFuelText.text = Mathf.Round(fuelTank.fuel).ToString();
			float subCurrFuel = 0f;
			float subMaxFuel = -1f;
			fuelTank.GetSubFuelInfo(out subCurrFuel, out subMaxFuel);
			float x = 0f;
			if (subMaxFuel > 0f)
			{
				x = subCurrFuel / subMaxFuel;
			}
			extFuelTf.localScale = new Vector3(x, 1f, 1f);
			extFuelText.text = Mathf.Round(subCurrFuel).ToString();
		}
	}
}
