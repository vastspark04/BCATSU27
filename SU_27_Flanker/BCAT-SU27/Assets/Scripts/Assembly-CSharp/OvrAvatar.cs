using UnityEngine;

public class OvrAvatar : MonoBehaviour
{
	public OvrAvatarMaterialManager DefaultBodyMaterialManager;
	public OvrAvatarMaterialManager DefaultHandMaterialManager;
	public OvrAvatarDriver Driver;
	public string oculusUserID;
	public bool EnableBody;
	public bool EnableHands;
	public bool EnableBase;
	public bool EnableExpressive;
	public bool RecordPackets;
	public bool UseSDKPackets;
	public PacketRecordSettings PacketSettings;
	public bool StartWithControllers;
	public AvatarLayer FirstPersonLayer;
	public AvatarLayer ThirdPersonLayer;
	public bool ShowFirstPerson;
	public bool ShowThirdPerson;
	public bool CanOwnMicrophone;
	public bool UseTransparentRenderQueue;
	public Shader Monochrome_SurfaceShader;
	public Shader Monochrome_SurfaceShader_SelfOccluding;
	public Shader Monochrome_SurfaceShader_PBS;
	public Shader Skinshaded_SurfaceShader_SingleComponent;
	public Shader Skinshaded_VertFrag_SingleComponent;
	public Shader Skinshaded_VertFrag_CombinedMesh;
	public Shader Skinshaded_Expressive_SurfaceShader_SingleComponent;
	public Shader Skinshaded_Expressive_VertFrag_SingleComponent;
	public Shader Skinshaded_Expressive_VertFrag_CombinedMesh;
	public Shader Loader_VertFrag_CombinedMesh;
	public Shader EyeLens;
	public GameObject MouthAnchor;
	public Transform LeftHandCustomPose;
	public Transform RightHandCustomPose;
	public float VoiceAmplitude;
	public bool EnableMouthVertexAnimation;
}
