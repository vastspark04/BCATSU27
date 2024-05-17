using UnityEngine;

public class CloudRotator : MonoBehaviour
{
	public string rotationPropertyName = "_CloudRotation";

	private Matrix4x4 cloudMatrix;

	private float lastAng;

	private float uang;

	private void OnPreRender()
	{
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		Vector3 toDirection = Vector3.ProjectOnPlane(base.transform.up, forward);
		Vector3 referenceRight = Vector3.Cross(Vector3.up, forward);
		float num = VectorUtils.SignedAngle(Vector3.up, toDirection, referenceRight);
		Vector3 vector = new Vector3(-0.5f, -0.5f, 0f);
		Vector3 vector2 = Quaternion.AngleAxis(0f - num, Vector3.forward) * vector - vector;
		Matrix4x4 matrix4x = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(0f - num, Vector3.forward), Vector3.one);
		Vector3 right = base.transform.right;
		right.y = 0f;
		float num2 = Mathf.Sign(Vector3.Dot(base.transform.forward, Vector3.up));
		float num3 = VectorUtils.SignedAngle(Vector3.forward, right, Vector3.right);
		float num4 = num3 - lastAng;
		lastAng = num3;
		uang += num2 * num4;
		if (uang > 180f)
		{
			uang -= 360f;
		}
		else if (uang < -180f)
		{
			uang += 360f;
		}
		num3 = uang;
		Matrix4x4 matrix4x2 = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(num3, Vector3.forward), Vector3.one);
		Vector3 vector3 = Quaternion.AngleAxis(num3, Vector3.forward) * vector - vector;
		Vector4 column = new Vector4(matrix4x2.m01, matrix4x2.m11, vector3.y, 0f);
		Vector4 column2 = new Vector4(matrix4x2.m00, matrix4x2.m10, vector3.x, 0f);
		Vector4 column3 = new Vector4(matrix4x.m01, matrix4x.m11, vector2.y, 0f);
		Vector4 column4 = new Vector4(matrix4x.m00, matrix4x.m10, vector2.x, 0f);
		cloudMatrix = new Matrix4x4(column4, column3, column2, column);
		Shader.SetGlobalMatrix(rotationPropertyName, cloudMatrix);
	}

	private void OnGUI()
	{
		Matrix4x4 matrix4x = cloudMatrix;
		string text = $"[\t{matrix4x.m00:0.0}\t][\t{matrix4x.m10:0.0}\t][\t{matrix4x.m20:0.0}\t][\t{matrix4x.m30:0.0}\t]\n[\t{matrix4x.m01:0.0}\t][\t{matrix4x.m11:0.0}\t][\t{matrix4x.m21:0.0}\t][\t{matrix4x.m31:0.0}\t]\n[\t{matrix4x.m02:0.0}\t][\t{matrix4x.m12:0.0}\t][\t{matrix4x.m22:0.0}\t][\t{matrix4x.m32:0.0}\t]\n[\t{matrix4x.m03:0.0}\t][\t{matrix4x.m13:0.0}\t][\t{matrix4x.m23:0.0}\t][\t{matrix4x.m33:0.0}\t]";
		GUI.Label(new Rect(10f, 10f, 800f, 800f), text);
	}
}
