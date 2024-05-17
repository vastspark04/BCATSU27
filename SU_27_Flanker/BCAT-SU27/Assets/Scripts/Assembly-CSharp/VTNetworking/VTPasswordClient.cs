using System;
using System.Runtime.InteropServices;
using System.Text;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace VTNetworking{

public class VTPasswordClient : ConnectionManager
{
	private VTPasswordAttempt currentAttempt;

	private byte[] pwBuffer = new byte[1024];

	private byte[] responseBuffer = new byte[8];

	public VTPasswordAttempt TryPassword(string pw, VTPasswordAttempt attempt)
	{
		if (currentAttempt == null)
		{
			currentAttempt = ((attempt != null) ? attempt : new VTPasswordAttempt());
			byte b = (byte)pw.Length;
			pwBuffer[0] = b;
			int bytes = Encoding.UTF8.GetBytes(pw, 0, pw.Length, pwBuffer, 1);
			Connection.SendMessage(pwBuffer, 0, bytes + 1, SendType.NoNagle | SendType.Reliable);
			return currentAttempt;
		}
		throw new InvalidOperationException("Only one password attempt can be made at a time!");
	}

	public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
	{
		if (currentAttempt != null)
		{
			Marshal.Copy(data, responseBuffer, 0, Mathf.Min(responseBuffer.Length, size));
			if (responseBuffer[0] > 0)
			{
				currentAttempt.status = VTPasswordAttempt.Statuses.Valid;
			}
			else
			{
				currentAttempt.status = VTPasswordAttempt.Statuses.WrongPassword;
			}
			currentAttempt = null;
		}
	}

	public override void OnDisconnected(ConnectionInfo info)
	{
		base.OnDisconnected(info);
		if (currentAttempt != null)
		{
			currentAttempt.status = VTPasswordAttempt.Statuses.NoResponse;
			currentAttempt = null;
		}
	}
}

}