using UnityEngine;

public interface ICanHoldPassengers
{
	bool HasPassengerBay();

	int GetMaximumPassengers();

	Transform GetSeatTransform(int seatIdx);
}
