using UnityEngine;

public class OvrAvatarDriver : MonoBehaviour
{
	public enum PacketMode
	{
		SDK = 0,
		Unity = 1,
	}

	public PacketMode Mode;
}
