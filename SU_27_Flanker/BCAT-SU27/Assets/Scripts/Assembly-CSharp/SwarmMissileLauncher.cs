public class SwarmMissileLauncher : HPEquipOpticalML, IRippleWeapon
{
	public float[] rippleRates = new float[4] { 0f, 800f, 2000f, 4000f };

	private int rippleIdx;

	public float[] GetRippleRates()
	{
		return rippleRates;
	}

	public void SetRippleRateIdx(int idx)
	{
		rippleIdx = idx;
	}

	public int GetRippleRateIdx()
	{
		return rippleIdx;
	}
}
