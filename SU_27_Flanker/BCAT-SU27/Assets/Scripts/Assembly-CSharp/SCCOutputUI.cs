public class SCCOutputUI : SCCNodeUI
{
	public override void UpdateComponent()
	{
		if (inputPorts[0].connections.Count > 0)
		{
			conditionalEditor.SetRootNode(inputPorts[0].connections[0].nodeUI);
		}
		else
		{
			conditionalEditor.SetRootNode(null);
		}
	}
}
