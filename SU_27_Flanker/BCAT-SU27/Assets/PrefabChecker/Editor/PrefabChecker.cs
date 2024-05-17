using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

enum IconType {
	ok,
	alert,
}

public class PrefabChecker: EditorWindow {

	Texture2D iconCheck;
	Texture2D iconAlert;

	bool bShow = false;
    float ftime = 0;

    bool bIncludeInactive = false;

	[MenuItem ("Window/PrefabChecker")]
	static void Init()
	{
		EditorWindow.GetWindow (typeof (PrefabChecker));
	}

	void OnEnable()
	{
        ftime = Time.realtimeSinceStartup;
		EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
	//	EditorApplication.hierarchyWindowChanged += OnHierarchyWindowChanged;
		EditorApplication.update += OnGenericUpdate;
		iconCheck = AssetDatabase.LoadAssetAtPath ("Assets/PrefabChecker/Editor/Icon/check.png", typeof(Texture2D)) as Texture2D;
        iconAlert = AssetDatabase.LoadAssetAtPath ("Assets/PrefabChecker/Editor/Icon/alert.png", typeof(Texture2D)) as Texture2D;
	
	}

	void OnDisable()
	{
		EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyWindowItemOnGUI;
		//EditorApplication.hierarchyWindowChanged -= OnHierarchyWindowChanged;
		EditorApplication.update -= OnGenericUpdate;
		EditorApplication.RepaintHierarchyWindow ();
	}

	private void OnHierarchyWindowItemOnGUI(int instanceid, Rect selectionrect)
	{
        if ( Application.isPlaying )
            return;

		if ( ! bShow )
			return;

		//GUI.Label(selectionrect, "Foo " + instanceid);
		UnityEngine.Object obj =	EditorUtility.InstanceIDToObject (instanceid);
        if (obj == null)
            return;

		GameObject go = (GameObject)obj;
        if ( bIncludeInactive == false && go.activeInHierarchy == false)
			return;

		int nValueChangedCnt = 0;
        bool bComponentChanged = false;
		if ( /* PrefabUtility.GetPrefabType (obj) == PrefabType.ModelPrefabInstance || */ 
		     PrefabUtility.GetPrefabType (obj) == PrefabType.PrefabInstance ) 
		{
			PropertyModification[] modis =	PrefabUtility.GetPropertyModifications( obj );

			//Debug.Log ("========obj.name = " + obj.name + " , modis count = " + modis.Length );
			foreach( PropertyModification modi in modis )
			{
				UnityEngine.Object targetObj = modi.target;

                //Debug.Log ("modi.target = " + modi.target + ", type = " + modi.target.GetType() );
               //  Debug.Log ("name = "+ go.name + ", instanceid = " + instanceid + ", propertyPath= " + modi.propertyPath + ", target.name=" + modi.target.name + ", target => " + modi.target +", value = " + modi.value);
				
                if( targetObj == null )
                    continue;

                //Should be same both target of PropertyModification and instance
				if( targetObj.name != go.name )
					continue;

                if( modi.value.Length < 1 )
                    continue;

				Transform compTr;

				//Only check component type, not gameobject itself
				if( targetObj.GetType() == typeof(GameObject) )
				{
					compTr = go.transform;
				}
				else
				{
					Component[] comps =	go.GetComponents( targetObj.GetType() );

					if( comps.Length <= 0 )
						continue;

					compTr = comps[0].transform;
				}

				//if this instance is root, then ignore localpos, localrot.
                if( ( compTr.parent == null || PrefabUtility.GetPrefabType (compTr.parent.gameObject) != PrefabType.PrefabInstance) && 
                   ( modi.propertyPath.Contains("LocalPosition") ||  modi.propertyPath.Contains("LocalRotation")  ||  modi.propertyPath.Contains("m_Name") 
                    || modi.propertyPath.Contains("m_RootOrder") ||  modi.propertyPath.Contains("m_RootMapOrder") ))
				{
					continue;
				}

                if ( modi.propertyPath.Contains("m_IsActive" ) ) // if toggle active checkbox, it returns m_isactive property value =1, so ignore it.
                {
                    continue;
                }

				//Debug.Log ("name = "+ go.name + ", instanceid = " + instanceid + ", propertyPath= " + modi.propertyPath + ", target.name=" + modi.target.name + ", target => " + modi.target +", value = " + modi.value);
				nValueChangedCnt++;
			}

            if( IsComponentAddOrDeletedToPrefabInstance(obj) )
            {
                bComponentChanged = true;
            }

            if (nValueChangedCnt > 0 || bComponentChanged ) 
            {
                ShowIcon (IconType.alert, selectionrect);
            }
            else 
            {
                ShowIcon (IconType.ok, selectionrect);
            }
		}

	}

//	private void OnHierarchyWindowChanged()
//	{
//
//	}

    bool IsComponentAddOrDeletedToPrefabInstance( UnityEngine.Object preInstance )
    {
        GameObject prefab =  PrefabUtility.GetPrefabParent(preInstance) as GameObject;
       
        //Debug.Log("prefab = " + prefab.name + ", prefab type = " + prefab.GetType());

        Component[] oriComps =  prefab.GetComponents<Component>();
        GameObject insGO = preInstance as GameObject;

        Component[] insComps =  insGO.GetComponents<Component>();

        if (oriComps.Length != insComps.Length)
            return true;

        foreach (Component ori in oriComps)
        {
            bool bFound = false;
            foreach (Component ins in insComps)
            {
                if( ori == null || ins == null ) //Missing Monoscript case 
                {
                    break;
                }

                if(  ori.GetType().FullName == ins.GetType().FullName )
                {
                    bFound = true;
                }
            }

            if( ! bFound )
            {
                return true;
            }
        }
      
       return false;
    }

	
	private void OnGenericUpdate()
	{
		float curtime = Time.realtimeSinceStartup;
		//Debug.Log ("curtime=" + curtime);

		if ( curtime - ftime > 1.0f ) 
		{
			//Debug.Log (" EditorApplication.RepaintHierarchyWindow() ");
			EditorApplication.RepaintHierarchyWindow();
			ftime = curtime;
		}


	}

   
	void OnGUI()
	{
        GUILayout.Space (20);

        bIncludeInactive =  GUILayout.Toggle(bIncludeInactive, "Include inactive objects");
        if (GUILayout.Button ("Show", GUILayout.Height(30))) 
		{
			bShow = true;
			RepaintWindow();
		}

        GUILayout.Space (10);

        if (GUILayout.Button ("Hide", GUILayout.Height(30))) 
		{
			bShow = false;
			RepaintWindow();
		}
	}

	void RepaintWindow()
	{
		EditorApplication.RepaintHierarchyWindow ();
	}

	void ShowIcon( IconType type, Rect selectionrect )
	{
		Rect r = new Rect (selectionrect); 
		r.x = r.width - 2;
		r.width = 20;
		r.height = 20;

		if (type == IconType.ok) 
		{
			GUI.Label (r, iconCheck); 
		} 
		else if (type == IconType.alert) 
		{
			GUI.Label (r, iconAlert); 
		} 
		else 
		{
		}
	}
};