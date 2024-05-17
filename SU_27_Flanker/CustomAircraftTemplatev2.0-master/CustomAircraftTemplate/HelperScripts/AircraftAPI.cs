using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomAircraftTemplateSU27 
{
    public class PIDFixer : MonoBehaviour
    {

        public static void IncreaseFlightAssistYawP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.yawPID.kp = FA.yawPID.kp + 0.005f;
            FlightLogger.Log("fa yaw kp = " + FA.yawPID.kp);
        }
        public static void DecreaseFlightAssistYawP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.yawPID.kp = FA.yawPID.kp - 0.005f;
            FlightLogger.Log("fa yawPID kp = " + FA.yawPID.kp);
        }
        public static void IncreaseFlightAssistYawI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.yawPID.ki = FA.yawPID.ki + 0.005f;
            FlightLogger.Log("fa yaw ki = " + FA.yawPID.ki);
        }
        public static void DecreaseFlightAssistYawI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.yawPID.ki = FA.yawPID.ki - 0.005f;
            FlightLogger.Log("fa yawPID ki = " + FA.yawPID.ki);
        }
        public static void IncreaseFlightAssistYawD()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.yawPID.kd = FA.yawPID.kd + 0.005f;
            FlightLogger.Log("fa yaw kd = " + FA.yawPID.kd);
        }
        public static void DecreaseFlightAssistYawD()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.yawPID.kd = FA.yawPID.kd - 0.005f;
            FlightLogger.Log("fa yawPID kd = " + FA.yawPID.kd);
        }
        public static void IncreaseFlightAssistRollP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.rollPID.kp = FA.rollPID.kp + 0.05f;
            FlightLogger.Log("fa roll kp = " + FA.rollPID.kp);
        }
        public static void DecreaseFlightAssistRollP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.rollPID.kp = FA.rollPID.kp - 0.05f;
            FlightLogger.Log("fa roll kp = " + FA.rollPID.kp);
        }
        public static void IncreaseFlightAssistRollI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.rollPID.ki = FA.rollPID.ki + 0.05f;
            FlightLogger.Log("fa roll ki = " + FA.rollPID.ki);
        }
        public static void DecreaseFlightAssistRollI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.rollPID.ki = FA.rollPID.ki - 0.05f;
            FlightLogger.Log("fa roll ki = " + FA.rollPID.ki);
        }
        public static void IncreaseFlightAssistRollD()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.rollPID.kd = FA.rollPID.kd + 0.05f;
            FlightLogger.Log("fa roll kd = " + FA.rollPID.kd);
        }
        public static void DecreaseFlightAssistRollD()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFARP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.rollPID.kd = FA.rollPID.kd - 0.05f;
            FlightLogger.Log("fa roll kd = " + FA.rollPID.kd);
        }

        public static void IncreaseFlightAssistpitchP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFAPP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.pitchPID.kp = FA.pitchPID.kp + 0.05f;
            FlightLogger.Log("fa pitch kp = " + FA.pitchPID.kp);
        }
        public static void DecreaseFlightAssistpitchP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFAPP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.pitchPID.kp = FA.pitchPID.kp - 0.05f;
            FlightLogger.Log("fa pitch kp = " + FA.pitchPID.kp);
        }

        public static void IncreaseFlightAssistpitchI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFAPP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.pitchPID.ki = FA.pitchPID.ki + 0.05f;
            FlightLogger.Log("fa pitch ki = " + FA.pitchPID.ki);
        }
        public static void DecreaseFlightAssistpitchI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFAPP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.pitchPID.ki = FA.pitchPID.ki - 0.05f;
            FlightLogger.Log("fa pitch ki = " + FA.pitchPID.ki);
        }

        public static void IncreaseFlightAssistpitchD()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IFAPP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.pitchPID.kd = FA.pitchPID.kd + 0.05f;
            FlightLogger.Log("fa pitch kd = " + FA.pitchPID.kd);
        }
        public static void DecreaseFlightAssistpitchD()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DFAPP 0.0");


            FlightAssist FA = go.GetComponent<FlightAssist>();
            FA.pitchPID.kd = FA.pitchPID.kd - 0.05f;
            FlightLogger.Log("fa pitch kd = " + FA.pitchPID.kd);
        }


        public static void IncreaseAPAltP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.altitudePitchPID.kp = AP.altitudePitchPID.kp + 0.05f;
            FlightLogger.Log("ap alt pitch kp = " + AP.altitudePitchPID.kp);
        }
        public static void DecreaseAPAltP()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.altitudePitchPID.kp = AP.altitudePitchPID.kp - 0.05f;
            FlightLogger.Log("ap alt pitch kp = " + AP.altitudePitchPID.kp);
        }

        public static void IncreaseAPAltC()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.altitudeClimbPID.kp = AP.altitudeClimbPID.kp + 0.05f;
            FlightLogger.Log("ap alt climb kp = " + AP.altitudeClimbPID.kp);
        }
        public static void DecreaseAPAltC()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.altitudeClimbPID.kp = AP.altitudeClimbPID.kp - 0.05f;
            FlightLogger.Log("ap alt climb kp = " + AP.altitudeClimbPID.kp);
        }

        public static void IncreaseAPHEadingR()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IAltP 0.0");



            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.headingRollPID.kp = AP.headingRollPID.kp + 0.05f;
            FlightLogger.Log("ap heading r kp = " + AP.headingRollPID.kp);
        }
        public static void DecreaseAPHEadingR()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.headingRollPID.kp = AP.headingRollPID.kp - 0.05f;
            FlightLogger.Log("ap heading r kp = " + AP.headingRollPID.kp);
        }

        public static void IncreaseAPHeadingT()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("IAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.headingTurnPID.kp = AP.headingTurnPID.kp + 0.05f;
            FlightLogger.Log("ap hdg turn kp = " + AP.headingTurnPID.kp);
        }
        public static void DecreaseAPHeadingT()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();


            //Debug.Log("DAltP 0.0");


            VTOLAutoPilot AP = go.GetComponent<VTOLAutoPilot>();
            AP.headingTurnPID.kp = AP.headingTurnPID.kp - 0.05f;
            FlightLogger.Log("ap hdg turn kp = " + AP.headingTurnPID.kp);
        }
    }
    public class TGPSetter : MonoBehaviour
    {
        public static void SetTGPtoRadarUI()
        {
            GameObject go = VTOLAPI.GetPlayersVehicleGameObject();
            OpticalTargeter tgp = AircraftAPI.GetChildWithName(go, "TargetingPage", false).GetComponent<TargetingMFDPage>().opticalTargeter;
            AircraftAPI.GetChildWithName(go, "RadarUIController", false).GetComponent<MFDRadarUI>().tgp = tgp;
        }
    }

    class AircraftAPI
    {
        public static GameObject SEAT_ADJUST_POSE_BOUNDS;
        private static Texture2D MenuTexture;
        public static PlayerVehicle pvAircraft;
        private static GameObject vPrefab;
        private static MissileLauncher ML;
        private static GameObject mPrefab;

        public static void VehicleAdd()
        {
            //Debug.Log("VA1.0");

            Traverse.Create<VTResources>().Method("FinallyLoadExtVehicle", Main.pathToBundle, AircraftInfo.AircraftPrefabName).GetValue();
            
        }

        public static GameObject GetChildWithName(GameObject obj, string name, bool check)
        {

            //Debug.unityLogger.logEnabled = Main.logging;
            Transform[] children = obj.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (check) {
                 //Debug.Log("Looking for:" + name + ", Found:" + child.name); 
                }
                if (child.name == name || child.name == (name + "(Clone)"))
                {
                    return child.gameObject;
                }
            }


            return null;

        }

       
        
       



       
    }




}
