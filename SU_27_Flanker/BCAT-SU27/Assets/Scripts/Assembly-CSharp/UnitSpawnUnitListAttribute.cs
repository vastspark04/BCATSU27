public class UnitSpawnUnitListAttribute : UnitSpawnAttribute
{
	public string getLimitMethodName;

	public UnitSpawnUnitListAttribute(string name, string getLimitMethodName)
	{
		base.name = name;
		this.getLimitMethodName = getLimitMethodName;
	}
}
