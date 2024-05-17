using System.Collections.Generic;
using UnityEngine;

public class VTConditionalEvents
{
	public class ConditionalAction
	{
		public class ConditionalActionBlock
		{
			public string blockName;

			public ScenarioConditional conditional;

			public VTEventInfo eventActions;

			public List<ConditionalActionBlock> nestedBlocks;

			public List<ConditionalActionBlock> elseIfBlocks;

			public VTEventInfo elseActions;

			public bool Fire()
			{
				if (conditional.GetCondition())
				{
					if (eventActions != null)
					{
						eventActions.Invoke();
					}
					if (nestedBlocks != null)
					{
						foreach (ConditionalActionBlock nestedBlock in nestedBlocks)
						{
							nestedBlock.Fire();
						}
					}
					return true;
				}
				if (elseIfBlocks != null)
				{
					foreach (ConditionalActionBlock elseIfBlock in elseIfBlocks)
					{
						if (elseIfBlock.Fire())
						{
							return false;
						}
					}
				}
				if (elseActions != null)
				{
					elseActions.Invoke();
				}
				return false;
			}

			public ConfigNode SaveToConfigNode(string nodeName)
			{
				ConfigNode configNode = new ConfigNode(nodeName);
				configNode.SetValue("blockName", blockName);
				if (conditional != null)
				{
					configNode.AddNode(conditional.SaveToNode());
				}
				if (eventActions != null)
				{
					ConfigNode node = eventActions.SaveToNode("ACTIONS");
					configNode.AddNode(node);
				}
				if (nestedBlocks != null)
				{
					foreach (ConditionalActionBlock nestedBlock in nestedBlocks)
					{
						ConfigNode node2 = nestedBlock.SaveToConfigNode("NESTED_IF");
						configNode.AddNode(node2);
					}
				}
				if (elseIfBlocks != null)
				{
					foreach (ConditionalActionBlock elseIfBlock in elseIfBlocks)
					{
						ConfigNode node3 = elseIfBlock.SaveToConfigNode("ELSE_IF");
						configNode.AddNode(node3);
					}
				}
				if (elseActions != null)
				{
					ConfigNode node4 = elseActions.SaveToNode("ELSE_ACTIONS");
					configNode.AddNode(node4);
				}
				return configNode;
			}

			public void LoadFromConfigNode(ConfigNode blockNode, bool isElseIfBlock)
			{
				blockName = blockNode.GetValue("blockName");
				conditional = new ScenarioConditional();
				conditional.LoadFromNode(blockNode.GetNode("CONDITIONAL"));
				conditional.GatherReferences();
				eventActions = new VTEventInfo();
				if (blockNode.HasNode("ACTIONS"))
				{
					eventActions.LoadFromInfoNode(blockNode.GetNode("ACTIONS"));
				}
				if (blockNode.HasNode("NESTED_IF"))
				{
					nestedBlocks = new List<ConditionalActionBlock>();
					foreach (ConfigNode node in blockNode.GetNodes("NESTED_IF"))
					{
						ConditionalActionBlock conditionalActionBlock = new ConditionalActionBlock();
						conditionalActionBlock.LoadFromConfigNode(node, isElseIfBlock: false);
						nestedBlocks.Add(conditionalActionBlock);
					}
				}
				if (!isElseIfBlock)
				{
					elseIfBlocks = new List<ConditionalActionBlock>();
					if (blockNode.HasNode("ELSE_IF"))
					{
						foreach (ConfigNode node2 in blockNode.GetNodes("ELSE_IF"))
						{
							ConditionalActionBlock conditionalActionBlock2 = new ConditionalActionBlock();
							conditionalActionBlock2.LoadFromConfigNode(node2, isElseIfBlock: true);
							elseIfBlocks.Add(conditionalActionBlock2);
						}
					}
				}
				elseActions = new VTEventInfo();
				if (blockNode.HasNode("ELSE_ACTIONS"))
				{
					elseActions.LoadFromInfoNode(blockNode.GetNode("ELSE_ACTIONS"));
				}
			}
		}

		public int id;

		public string name;

		public List<ConditionalActionBlock> baseBlocks = new List<ConditionalActionBlock>();

		public const string NODE_NAME = "ConditionalAction";

		public void Fire()
		{
			if (baseBlocks == null)
			{
				return;
			}
			foreach (ConditionalActionBlock baseBlock in baseBlocks)
			{
				baseBlock.Fire();
			}
		}

		public ConfigNode SaveToConfigNode()
		{
			ConfigNode configNode = new ConfigNode("ConditionalAction");
			configNode.SetValue("id", id);
			configNode.SetValue("name", name);
			if (baseBlocks != null)
			{
				foreach (ConditionalActionBlock baseBlock in baseBlocks)
				{
					ConfigNode node = baseBlock.SaveToConfigNode("BASE_BLOCK");
					configNode.AddNode(node);
				}
				return configNode;
			}
			return configNode;
		}

		public void LoadFromConfigNode(ConfigNode node)
		{
			id = node.GetValue<int>("id");
			name = node.GetValue("name");
			baseBlocks = new List<ConditionalActionBlock>();
			foreach (ConfigNode node2 in node.GetNodes("BASE_BLOCK"))
			{
				ConditionalActionBlock conditionalActionBlock = new ConditionalActionBlock();
				conditionalActionBlock.LoadFromConfigNode(node2, isElseIfBlock: false);
				baseBlocks.Add(conditionalActionBlock);
			}
		}
	}

	public const string NODE_NAME = "ConditionalActions";

	private Dictionary<int, ConditionalAction> actions = new Dictionary<int, ConditionalAction>();

	private int nextID;

	public ConditionalAction CreateNewAction()
	{
		ConditionalAction conditionalAction = new ConditionalAction();
		conditionalAction.id = nextID++;
		actions.Add(conditionalAction.id, conditionalAction);
		ConditionalAction.ConditionalActionBlock conditionalActionBlock = new ConditionalAction.ConditionalActionBlock();
		conditionalActionBlock.conditional = new ScenarioConditional();
		conditionalActionBlock.eventActions = new VTEventInfo();
		conditionalActionBlock.elseIfBlocks = new List<ConditionalAction.ConditionalActionBlock>();
		conditionalActionBlock.elseActions = new VTEventInfo();
		conditionalAction.baseBlocks.Add(conditionalActionBlock);
		return conditionalAction;
	}

	public void DeleteAction(int id)
	{
		if (!actions.TryGetValue(id, out var value))
		{
			return;
		}
		foreach (ConditionalAction.ConditionalActionBlock baseBlock in value.baseBlocks)
		{
			DeleteEventTargets(baseBlock);
		}
		actions.Remove(id);
	}

	public void DeleteEventTargets(ConditionalAction.ConditionalActionBlock block)
	{
		foreach (VTEventTarget action in block.eventActions.actions)
		{
			action.DeleteEventTarget();
		}
		if (block.elseActions != null)
		{
			foreach (VTEventTarget action2 in block.elseActions.actions)
			{
				action2.DeleteEventTarget();
			}
		}
		if (block.elseIfBlocks == null)
		{
			return;
		}
		foreach (ConditionalAction.ConditionalActionBlock elseIfBlock in block.elseIfBlocks)
		{
			DeleteEventTargets(elseIfBlock);
		}
	}

	public ConditionalAction GetAction(int id)
	{
		if (actions.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void FireEvent(int id)
	{
		if (actions.TryGetValue(id, out var value))
		{
			value.Fire();
			actions.Remove(id);
		}
	}

	public void LoadFromScenarioNode(ConfigNode scenarioNode)
	{
		actions = new Dictionary<int, ConditionalAction>();
		if (!scenarioNode.HasNode("ConditionalActions"))
		{
			return;
		}
		foreach (ConfigNode node in scenarioNode.GetNode("ConditionalActions").GetNodes("ConditionalAction"))
		{
			ConditionalAction conditionalAction = new ConditionalAction();
			conditionalAction.LoadFromConfigNode(node);
			actions.Add(conditionalAction.id, conditionalAction);
			nextID = Mathf.Max(nextID, conditionalAction.id + 1);
			Debug.LogFormat("Loaded conditional action, ID:{0}", conditionalAction.id);
		}
	}

	public void SaveToScenarioNode(ConfigNode scenarioNode)
	{
		ConfigNode configNode = new ConfigNode("ConditionalActions");
		foreach (ConditionalAction value in actions.Values)
		{
			ConfigNode node = value.SaveToConfigNode();
			configNode.AddNode(node);
		}
		scenarioNode.AddNode(configNode);
	}
}
