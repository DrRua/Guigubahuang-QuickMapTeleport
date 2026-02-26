using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace MOD_aWVIMC
{
    public class ModMain
    {
        private TimerCoroutine corUpdate;
        private static HarmonyLib.Harmony harmony;
        private bool isMapOpen;
        private bool isCtrlPressed;

        public void Init()
        {
            MelonLogger.Msg("=== MOD_aWVIMC 开始初始化 ===");

            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
            if (harmony == null)
            {
                harmony = new HarmonyLib.Harmony("MOD_aWVIMC");
            }
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            corUpdate = g.timer.Frame(new Action(OnUpdate), 1, true);
            MelonLogger.Msg("=== MOD_aWVIMC 初始化完成，OnUpdate已注册 ===");
        }

        public void Destroy()
        {
            if (corUpdate != null)
            {
                g.timer.Stop(corUpdate);
            }
        }

        private void OnUpdate()
        {
            try
            {
                MelonLogger.Msg("--------------------------------");
                bool currentMapOpen = CheckIfMapIsOpen();
                bool currentCtrlState = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (currentMapOpen != isMapOpen)
                {
                    isMapOpen = currentMapOpen;
                    if (!isMapOpen)
                    {
                        isCtrlPressed = false;
                    }
                }

                if (isMapOpen && currentCtrlState != isCtrlPressed)
                {
                    isCtrlPressed = currentCtrlState;
                    if (isCtrlPressed)
                    {
                        MelonLogger.Msg("Ctrl键已按下 - 隐藏非城镇/宗门地图点");
                    }
                    else
                    {
                        MelonLogger.Msg("Ctrl键已释放 - 显示所有地图点");
                    }
                    UpdateMapIcons();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.LogError("Error in OnUpdate: " + ex.Message);
            }
        }

        private bool CheckIfMapIsOpen()
        {
            try
            {
                var uiMgr = g.ui;
                if (uiMgr == null)
                {
                    return false;
                }

                var openUIs = uiMgr.GetType().GetField("m_OpenUI", BindingFlags.NonPublic | BindingFlags.Instance);
                if (openUIs != null)
                {
                    var uiList = openUIs.GetValue(uiMgr) as IList;
                    if (uiList != null)
                    {
                        IEnumerator uiEnumerator = uiList.GetEnumerator();
                        while (uiEnumerator.MoveNext())
                        {
                            var uiItem = uiEnumerator.Current;
                            var uiBase = uiItem.GetType().GetProperty("Value")?.GetValue(uiItem);
                            if (uiBase != null)
                            {
                                string uiName = uiBase.GetType().Name;
                                if (uiName.Contains("WorldMap") || uiName.Contains("MiniMap"))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.LogError("Error checking map state: " + ex.Message);
            }
            return false;
        }

        private void UpdateMapIcons()
        {
            if (!isMapOpen)
            {
                return;
            }

            try
            {
                SetMapPointsVisible(!isCtrlPressed);
            }
            catch (Exception ex)
            {
                MelonLogger.LogError("Error updating map icons: " + ex.Message);
            }
        }

        private void SetMapPointsVisible(bool visible)
        {
            try
            {
                var uiMgr = g.ui;
                if (uiMgr == null)
                {
                    return;
                }

                var openUIs = uiMgr.GetType().GetField("m_OpenUI", BindingFlags.NonPublic | BindingFlags.Instance);
                if (openUIs != null)
                {
                    var uiList = openUIs.GetValue(uiMgr) as IList;
                    if (uiList != null)
                    {
                        IEnumerator uiEnumerator = uiList.GetEnumerator();
                        while (uiEnumerator.MoveNext())
                        {
                            var uiItem = uiEnumerator.Current;
                            var uiBase = uiItem.GetType().GetProperty("Value")?.GetValue(uiItem);
                            if (uiBase != null)
                            {
                                string uiName = uiBase.GetType().Name;
                                if (uiName.Contains("WorldMap") || uiName.Contains("MiniMap"))
                                {
                                    ProcessMapUI(uiBase, visible);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.LogError("Error setting map points visible: " + ex.Message);
            }
        }

        private void ProcessMapUI(object mapUI, bool visible)
        {
            try
            {
                var allPointsField = mapUI.GetType().GetField("allMapPoints", BindingFlags.NonPublic | BindingFlags.Instance);
                if (allPointsField != null)
                {
                    var points = allPointsField.GetValue(mapUI) as IList;
                    if (points != null)
                    {
                        IEnumerator pointsEnumerator = points.GetEnumerator();
                        while (pointsEnumerator.MoveNext())
                        {
                            var point = pointsEnumerator.Current;
                            var gameObjectProperty = point.GetType().GetProperty("gameObject");
                            if (gameObjectProperty != null)
                            {
                                var pointObj = gameObjectProperty.GetValue(point) as GameObject;
                                if (pointObj != null)
                                {
                                    var dataField = point.GetType().GetField("data", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (dataField != null)
                                    {
                                        var mapPointData = dataField.GetValue(point);
                                        if (mapPointData != null)
                                        {
                                            int pointType = 0;
                                            var typeField = mapPointData.GetType().GetField("type");
                                            if (typeField != null)
                                            {
                                                pointType = (int)typeField.GetValue(mapPointData);
                                            }

                                            bool isTownOrFaction = (pointType == 1 || pointType == 2);

                                            if (!visible)
                                            {
                                                pointObj.SetActive(isTownOrFaction);
                                            }
                                            else
                                            {
                                                pointObj.SetActive(true);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.LogError("Error processing map UI: " + ex.Message);
            }
        }
    }
}
