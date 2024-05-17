public class UnitSpawnAttributeURef : UnitSpawnAttribute
{
	public TeamOptions teamOption;

	public bool allowSubunits;

	public UnitSpawnAttributeURef(string name, TeamOptions teamOption, bool allowSubunits = false)
	{
		base.name = name;
		this.teamOption = teamOption;
		this.allowSubunits = allowSubunits;
	}
}
