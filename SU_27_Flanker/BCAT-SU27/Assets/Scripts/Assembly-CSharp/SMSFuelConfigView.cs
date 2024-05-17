using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SMSFuelConfigView : MonoBehaviour
{
	public VehicleMaster vm;

	public FuelTank internalFuelTank;

	public Transform bingoBar;

	public RectTransform internalFuelBar;

	public Text internalFuelText;

	public RectTransform externalFuelBar;

	public Text externalFuelText;

	public Text bingoFuelText;

	public Text bingoEstimationText;

	public float lowUsage;

	public float highUsage;

	public MeasurementManager measurements;

	public float bingoAdjustAccel = 0.1f;

	private float bingoAdjustRate;

	private Coroutine bingoAdjRoutine;

	private void Start()
	{
		vm.OnSetNormBingoFuel += Vm_OnSetNormBingoFuel;
		Vm_OnSetNormBingoFuel(vm.normBingoLevel);
	}

	private void Vm_OnSetNormBingoFuel(float normBingoFuel)
	{
		Vector3 localPosition = bingoBar.localPosition;
		localPosition.x = internalFuelBar.localPosition.x + internalFuelBar.rect.width * normBingoFuel;
		bingoBar.localPosition = localPosition;
		float num = normBingoFuel * internalFuelTank.maxFuel;
		bingoFuelText.text = Mathf.Round(num).ToString();
		float distance = Mathf.Round(num * highUsage);
		float distance2 = Mathf.Round(num * lowUsage);
		string text = $"{measurements.FormattedDistance(distance)} - {measurements.FormattedDistance(distance2)}";
		bingoEstimationText.text = text;
	}

	private void OnEnable()
	{
		StartCoroutine(FuelLevelUpdate());
	}

	private IEnumerator FuelLevelUpdate()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		yield return null;
		while (base.enabled)
		{
			internalFuelBar.localScale = new Vector3(internalFuelTank.fuelFraction, 1f, 1f);
			internalFuelText.text = Mathf.Round(internalFuelTank.fuel).ToString();
			internalFuelTank.GetSubFuelInfo(out var subCurrFuel, out var subMaxFuel);
			float x = 0f;
			if (subMaxFuel > 0f)
			{
				x = subCurrFuel / subMaxFuel;
			}
			externalFuelBar.localScale = new Vector3(x, 1f, 1f);
			externalFuelText.text = Mathf.Round(subMaxFuel).ToString();
			yield return wait;
		}
	}

	public void IncreaseBingoFuel()
	{
		bingoAdjRoutine = StartCoroutine(BingoAdjustRoutine(1));
	}

	public void DecreaseBingoFuel()
	{
		bingoAdjRoutine = StartCoroutine(BingoAdjustRoutine(-1));
	}

	public void StopBingoInput()
	{
		bingoAdjustRate = 0f;
		if (bingoAdjRoutine != null)
		{
			StopCoroutine(bingoAdjRoutine);
		}
	}

	private IEnumerator BingoAdjustRoutine(int dir)
	{
		while (base.enabled)
		{
			vm.normBingoLevel += (float)dir * bingoAdjustRate * Time.deltaTime;
			bingoAdjustRate += bingoAdjustAccel * Time.deltaTime;
			yield return null;
		}
	}
}
