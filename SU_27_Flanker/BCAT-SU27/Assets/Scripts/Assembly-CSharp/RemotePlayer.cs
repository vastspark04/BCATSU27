using Oculus.Platform;
using UnityEngine;

public class RemotePlayer
{
	public ulong remoteUserID;
	public bool stillInRoom;
	public PeerConnectionState p2pConnectionState;
	public PeerConnectionState voipConnectionState;
	public OvrAvatar RemoteAvatar;
	public Vector3 receivedRootPosition;
	public Vector3 receivedRootPositionPrior;
	public Quaternion receivedRootRotation;
	public Quaternion receivedRootRotationPrior;
	public VoipAudioSourceHiLevel voipSource;
}
