public interface ITargetPreferences
{
	void SetNonTargets(UnitReferenceList list);

	void AddNonTargets(UnitReferenceList list);

	void RemoveNonTargets(UnitReferenceList list);

	void ClearNonTargets();

	void SetPriorityTargets(UnitReferenceList list);

	void AddPriorityTargets(UnitReferenceList list);

	void RemovePriorityTargets(UnitReferenceList list);

	void ClearPriorityTargets();
}
