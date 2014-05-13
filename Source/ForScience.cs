using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace ForScience
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ForScience : MonoBehaviour
    {
        //GUI
        private GUIStyle windowStyle, labelStyle, toggleStyle;
        private Rect windowPosition = new Rect(0f, 200f, 0f, 0f);
        private bool initStyle = false;

        // states

        bool initState = false;
        Vessel stateVessel = null;
        CelestialBody stateBody = null;
        string stateBiome = null;
        ExperimentSituations stateSituation = 0;

        //global variables

        bool IsDataToCollect = false;
        bool autoTransfer = false;
        bool dataIsInContainer = false;
        Vessel currentVessel = null;
        CelestialBody currentBody = null;
        string currentBiome = null;
        ExperimentSituations currentSituation = 0;
        List<ModuleScienceContainer> containerList = null;
        List<ModuleScienceExperiment> experimentList = null;
        ModuleScienceContainer container = null;


        private void Start()
        {
            if (!initStyle) InitStyle();
            RenderingManager.AddToPostDrawQueue(0, OnDraw);

            if (!initState)
            {
                UpdateStates();
            }
        }

        private void Update()
        {
            UpdateCurrent();

            if (!currentVessel.isEVA & autoTransfer & IsDataToCollect) ManageScience();

            if (!currentVessel.isEVA & autoTransfer & (currentVessel != stateVessel | currentSituation != stateSituation | currentBody != stateBody | currentBiome != stateBiome))
            {
                RunScience();
                UpdateStates();
            }
        }

        private void OnDraw()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                windowPosition = GUILayout.Window(104234, windowPosition, MainWindow, "For Science", windowStyle);
            }
        }

        private void ManageScience()
        {
            containerList = currentVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            experimentList = currentVessel.FindPartModulesImplementing<ModuleScienceExperiment>();

            if (container == null) container = containerList[0];
            container.StoreData(experimentList.Cast<IScienceDataContainer>().ToList(), true);
        }

        
        private void RunScience()
        {


            //foreach (var id in ResearchAndDevelopment.GetExperimentIDs())
            //{
            //    Debug.Log(String.Format("4S: R&D: Expiroment ID: {0}", id));
            //}
            foreach (var id in ResearchAndDevelopment.GetBiomeTags(currentBody))
            {
                Debug.Log(String.Format("4S: R&D: Biome Tag: {0}", id));
            }
            foreach (var id in ResearchAndDevelopment.GetSubjects())
            {
                Debug.Log(String.Format("4S: R&D: Subject: {0} {1} {2} {3} {4} {5} {6}", id.title, id.id, id.dataScale, id.science, id.scienceCap, id.scientificValue, id.subjectValue));
            }

            var subjects = ResearchAndDevelopment.GetSubjects().Select(z => new { id = z.id, value = z.scientificValue }).ToLookup(i => i.id, i => i.value);


            foreach (var id in subjects)
            {
                Debug.Log(String.Format("4S: Subject: {0}  Science Left: {1}", id.Key, id.First()));
            }

            Debug.Log(String.Format("4S: {0} flyingAltitudeThreshold: {1}", currentBody.name, currentBody.scienceValues.flyingAltitudeThreshold));
            Debug.Log(String.Format("4S: {0} FlyingHighDataValue: {1}", currentBody.name, currentBody.scienceValues.FlyingHighDataValue));
            Debug.Log(String.Format("4S: {0} FlyingLowDataValue: {1}", currentBody.name, currentBody.scienceValues.FlyingLowDataValue));
            Debug.Log(String.Format("4S: {0} InSpaceHighDataValue: {1}", currentBody.name, currentBody.scienceValues.InSpaceHighDataValue));
            Debug.Log(String.Format("4S: {0} InSpaceLowDataValue: {1}", currentBody.name, currentBody.scienceValues.InSpaceLowDataValue));
            Debug.Log(String.Format("4S: {0} LandedDataValue: {1}", currentBody.name, currentBody.scienceValues.LandedDataValue));
            Debug.Log(String.Format("4S: {0} RecoveryValue: {1}", currentBody.name, currentBody.scienceValues.RecoveryValue));
            Debug.Log(String.Format("4S: {0} spaceAltitudeThreshold: {1}", currentBody.name, currentBody.scienceValues.spaceAltitudeThreshold));
            Debug.Log(String.Format("4S: {0} SplashedDataValue: {1}", currentBody.name, currentBody.scienceValues.SplashedDataValue));

            var experiments = experimentList.GroupBy(z => z.experimentID).Select(i => i.First());

            foreach (ModuleScienceExperiment currentExperiment in experiments)
            {
                if (currentExperiment.rerunnable & currentExperiment.experiment.IsAvailableWhile(currentSituation, currentBody))
                {
                    Debug.Log("4S: ---------------------------------------------------------");

                    Debug.Log("4S: checking experiment: " + currentExperiment.name);

                    dataIsInContainer = false;

                    foreach (ScienceData data in container.GetData())
                    {
                        Debug.Log(String.Format("4S: Have: {0} with science value of {1} {2} {3}",data.subjectID, data.dataAmount, data.labBoost, data.transmitValue));

                        if (data.subjectID == (currentExperiment.experimentID + "@" + currentBody.name + currentSituation + currentVessel.landedAt).Replace(" ", string.Empty))
                        {
                            //Debug.Log("4S: experiment: " + (currentExperiment.experimentID + "@" + currentBody.name + currentSituation + currentVessel.landedAt).Replace(" ", string.Empty));
                            Debug.Log("4S: Already have a:" + data.subjectID);
                            dataIsInContainer = true;
                        }
                        else if (data.subjectID == (currentExperiment.experimentID + "@" + currentBody.name + currentSituation + currentBiome).Replace(" ", string.Empty))
                        {
                            //Debug.Log("4S: experiment: " + (currentExperiment.experimentID + "@" + currentBody.name + currentSituation + currentBiome).Replace(" ", string.Empty));
                            Debug.Log("4S: Already have a:" + data.subjectID);
                            dataIsInContainer = true;
                        }
                        else if (data.subjectID == (currentExperiment.experimentID + "@" + currentBody.name + currentSituation).Replace(" ", string.Empty))
                        {
                            //Debug.Log("4S: experiment: " + (currentExperiment.experimentID + "@" + currentBody.name + currentSituation).Replace(" ", string.Empty));
                            Debug.Log("4S: Already have a:" + data.subjectID);
                            dataIsInContainer = true;
                        }
                    }
                    if (!dataIsInContainer & currentExperiment.GetScienceCount() == 0)
                    {
                        var exp = ResearchAndDevelopment.GetExperiment(currentExperiment.experimentID);
                        var sub = ResearchAndDevelopment.GetExperimentSubject(currentExperiment.experiment, currentSituation, currentBody, currentBiome);

                        if (subjects[sub.id].First() <= 0f) return;

                        var science = ResearchAndDevelopment.GetScienceValue(currentExperiment.xmitDataScalar, sub);

                        var xA = ResearchAndDevelopment.GetScienceValue((float)0, sub);
                        var xB = ResearchAndDevelopment.GetScienceValue((float)10, sub);
                        var xC = ResearchAndDevelopment.GetScienceValue((float)50, sub);
                        var xD = ResearchAndDevelopment.GetScienceValue((float)100, sub);
                        var xE = ResearchAndDevelopment.GetNextScienceValue((float)0, sub);
                        var xF = ResearchAndDevelopment.GetNextScienceValue((float)10, sub);
                        var xG = ResearchAndDevelopment.GetNextScienceValue((float)50, sub);
                        var xH= ResearchAndDevelopment.GetNextScienceValue((float)100, sub);

                        var rA= ResearchAndDevelopment.GetReferenceDataValue((float)0,sub);
                        var rB= ResearchAndDevelopment.GetReferenceDataValue((float)10,sub);
                        var rC= ResearchAndDevelopment.GetReferenceDataValue((float)50,sub);
                        var rD= ResearchAndDevelopment.GetReferenceDataValue((float)100,sub);
   
                        Debug.Log(String.Format("4S: Running experiment: {0} ({1}) {2} ", sub.title, exp.id, sub.id));

                        Debug.Log(String.Format("4S: xmit:  {0}", currentExperiment.xmitDataScalar ));
                        Debug.Log(String.Format("4S: 0 science:  {0} {1}", xA, xE));
                        Debug.Log(String.Format("4S: 10 science:  {0} {1}", xB, xF));
                        Debug.Log(String.Format("4S: 50 science:  {0} {1}",  xC, xG));
                        Debug.Log(String.Format("4S: 100 science: {0} {1}",  xD, xH));
                        Debug.Log(String.Format("4S: value?: {0}", science));

                        Debug.Log(String.Format("4S: 0 ref:  {0}", rA));
                        Debug.Log(String.Format("4S: 10 ref:  {0}", rB));
                        Debug.Log(String.Format("4S: 50 ref:  {0}",  rC));
                        Debug.Log(String.Format("4S: 100 ref: {0}",  rD));

                        //Debug.Log(String.Format("4S: sub.title: {0}", sub.title));
                        //Debug.Log(String.Format("4S: sub.id: {0}", sub.id));
                        Debug.Log(String.Format("4S: sub {0}, {1}, {2}, {3}, {4}, exp {5}, {6}, {7}", sub.dataScale, sub.science, sub.scienceCap, sub.scientificValue, sub.subjectValue, exp.baseValue, exp.dataScale, exp.scienceCap));

                        var data = currentExperiment.GetData();
                        Debug.Log(String.Format("4S: Data Count: {0}",data.Count()));
                        foreach (var d in data)
                        {
                            Debug.Log(String.Format("4S: pre-data:  {0} {1} {2} {3}", d.subjectID, d.title, d.labBoost, d.dataAmount, d.transmitValue));
                        }


                        currentExperiment.DeployExperiment();

                        data = currentExperiment.GetData();
                        Debug.Log(String.Format("4S: Data Count: {0}", data.Count()));
                        foreach (var d in data)
                        {
                            Debug.Log(String.Format("4S: post-data:  {0} {1} {2} {3}", d.subjectID, d.title, d.labBoost, d.dataAmount, d.transmitValue));
                        }

                        IsDataToCollect = true;
                    }
                }
            }
        }

        private void UpdateCurrent()
        {
            currentVessel = FlightGlobals.ActiveVessel;
            currentBody = currentVessel.mainBody;
            currentBiome = ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);
            currentSituation = ScienceUtil.GetExperimentSituation(currentVessel);
        }

        private void UpdateStates()
        {
            stateVessel = FlightGlobals.ActiveVessel;
            stateBody = currentVessel.mainBody;
            stateBiome = ScienceUtil.GetExperimentBiome(currentBody, currentVessel.latitude, currentVessel.longitude);
            stateSituation = ScienceUtil.GetExperimentSituation(currentVessel);

            initState = true;
        }

        private void InitStyle()
        {
            labelStyle = new GUIStyle(HighLogic.Skin.label);
            labelStyle.stretchWidth = true;

            windowStyle = new GUIStyle(HighLogic.Skin.window);
            windowStyle.fixedWidth = 250f;

            toggleStyle = new GUIStyle(HighLogic.Skin.toggle);

            initStyle = true;
        }

        private void MainWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(autoTransfer, "Automatic data collection", toggleStyle))
            {
                if (!autoTransfer)
                {
                    ManageScience();
                    RunScience();
                }
                autoTransfer = true;
            }
            else autoTransfer = false;
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }
    }
}






