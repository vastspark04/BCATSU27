using UnityEngine;

public struct EqInfo
{
	public GameObject eqObject;

	public HPEquippable eq;

	public string prefabPath;

	public int compatibilityMask;

	public EqInfo(GameObject equipObject, string path)
	{
		prefabPath = path;
		eqObject = equipObject;
		eq = equipObject.GetComponentImplementing<HPEquippable>();
		compatibilityMask = LoadoutConfigurator.EquipCompatibilityMask(eq);
	}

	public GameObject GetInstantiated()
	{
		GameObject obj = (GameObject)Object.Instantiate(Resources.Load(prefabPath));
		obj.name = eqObject.name;
		obj.SetActive(value: true);
		return obj;
	}

	public bool IsCompatibleWithHardpoint(int hpIdx)
	{
		int num = 1 << hpIdx;
		return (compatibilityMask & num) == num;
	}
}
