﻿/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

namespace NRKernal
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using NRKernal.Record;

#if USING_XR_SDK 
    using UnityEngine.XR;
    using System.Runtime.InteropServices;
#endif

    /// <summary>
    /// Manages AR system state and handles the session lifecycle. this class, application can create
    /// a session, configure it, start/pause or stop it. </summary>
    public class NRSessionManager : SingleTon<NRSessionManager>
    {
        /// <summary> The lost tracking reason. </summary>
        private LostTrackingReason m_LostTrackingReason = LostTrackingReason.INITIALIZING;

        /// <summary> Current lost tracking reason. </summary>
        /// <value> The lost tracking reason. </value>
        public LostTrackingReason LostTrackingReason
        {
            get
            {
                return m_LostTrackingReason;
            }
        }

        /// <summary> State of the session. </summary>
        private SessionState m_SessionState = SessionState.UnInitialized;

        /// <summary> Gets or sets the state of the session. </summary>
        /// <value> The session state. </value>
        public SessionState SessionState
        {
            get
            {
                return m_SessionState;
            }
            private set
            {
                m_SessionState = value;
            }
        }

        /// <summary> Gets or sets the nr session behaviour. </summary>
        /// <value> The nr session behaviour. </value>
        public NRSessionBehaviour NRSessionBehaviour { get; private set; }

        /// <summary> Gets or sets the nrhmd pose tracker. </summary>
        /// <value> The nrhmd pose tracker. </value>
        public NRHMDPoseTracker NRHMDPoseTracker { get; private set; }

        /// <summary> Gets or sets the native a pi. </summary>
        /// <value> The native a pi. </value>
        public NativeInterface NativeAPI { get; private set; }

        /// <summary> Gets or sets the nr renderer. </summary>
        /// <value> The nr renderer. </value>
        public NRRenderer NRRenderer { get; set; }

        /// <summary> Gets or sets the virtual displayer. </summary>
        /// <value> The virtual displayer. </value>
        public NRVirtualDisplayer VirtualDisplayer { get; set; }

        public NRTrackingModeChangedListener TrackingLostListener { get; set; }

        /// <summary> Gets the center camera anchor. </summary>
        /// <value> The center camera anchor. </value>
        public Transform CenterCameraAnchor
        {
            get
            {
                if (NRHMDPoseTracker != null && NRHMDPoseTracker.centerAnchor != null)
                {
                    return NRHMDPoseTracker.centerAnchor;
                }
                else
                {
                    return Camera.main.transform;
                }
            }
        }

        /// <summary> Gets the left camera anchor. </summary>
        /// <value> The left camera anchor. </value>
        public Transform LeftCameraAnchor
        {
            get
            {
                return NRHMDPoseTracker?.leftCamera.transform;
            }
        }

        /// <summary> Gets the right camera anchor. </summary>
        /// <value> The right camera anchor. </value>
        public Transform RightCameraAnchor
        {
            get
            {
                return NRHMDPoseTracker?.rightCamera.transform;
            }
        }

        #region Events
        public delegate void SessionError(NRKernalError exception);

        /// <summary> Event queue for all listeners interested in OnHMDPoseReady events. </summary>
        public static HMDPoseTrackerEvent OnHMDPoseReady;
        /// <summary> Event queue for all listeners interested in OnHMDLostTracking events. </summary>
        public static HMDPoseTrackerEvent OnHMDLostTracking;
        /// <summary> Event queue for all listeners interested in OnChangeTrackingMode events. </summary>
        public static HMDPoseTrackerModeChangeEvent OnChangeTrackingMode;

        /// <summary> Event queue for all listeners interested in OnGlassesStateChanged events. </summary>
        public static GlassesEvent OnGlassesStateChanged;
        /// <summary> Event queue for all listeners interested in OnGlassesDisconnect events. </summary>
        public static GlassesDisconnectEvent OnGlassesDisconnect;

        /// <summary> Event queue for all listeners interested in kernal error, 
        /// such as NRRGBCameraDeviceNotFindError, NRPermissionDenyError, NRUnSupportedHandtrackingCalculationError. </summary>
        public static event SessionError OnKernalError;
        #endregion

#if USING_XR_SDK
        public const string display_match = "NRSDK Display";
        public const string input_match = "NRSDK Head Tracking";
        
        private XRDisplaySubsystem m_XRDisplaySubsystem;
        public XRDisplaySubsystem XRDisplaySubsystem{
            get{
                if (m_XRDisplaySubsystem == null)
                {
                    List<XRDisplaySubsystemDescriptor> displays = new List<XRDisplaySubsystemDescriptor>();
                    SubsystemManager.GetSubsystemDescriptors(displays);

                    foreach (var d in displays)
                    {
                        if (d.id.Contains(display_match))
                        {
                            m_XRDisplaySubsystem = d.Create();
                        }
                    }
                }
                return m_XRDisplaySubsystem;
            }
        }

        private XRInputSubsystem m_XRInputSubsystem;
        public XRInputSubsystem XRInputSubsystem
        {
            get{
                if (m_XRInputSubsystem == null)
                {
                    List<XRInputSubsystemDescriptor> inputs = new List<XRInputSubsystemDescriptor>();
                    SubsystemManager.GetSubsystemDescriptors(inputs);

                    foreach (var i in inputs)
                    {
                        if (i.id.Contains(input_match))
                        {
                            m_XRInputSubsystem = i.Create();
                        }
                    }
                }
                return m_XRInputSubsystem;
            }
        }
#endif

        /// <summary> Gets a value indicating whether this object is initialized. </summary>
        /// <value> True if this object is initialized, false if not. </value>
        public bool IsInitialized
        {
            get
            {
                return (SessionState != SessionState.UnInitialized
                    && SessionState != SessionState.Destroyed);
            }
        }

        /// <summary> Creates a session. </summary>
        /// <param name="session"> The session behaviour.</param>
        public void CreateSession(NRSessionBehaviour session)
        {
            if (SessionState != SessionState.UnInitialized && SessionState != SessionState.Destroyed)
            {
                return;
            }

            SetAppSettings();

            if (NRSessionBehaviour != null)
            {
                NRDebugger.Error("[NRSessionManager] Multiple SessionBehaviour components cannot exist in the scene. " +
                  "Destroying the newest.");
                GameObject.DestroyImmediate(session.gameObject);
                return;
            }
            NRSessionBehaviour = session;
            NRHMDPoseTracker = session.GetComponent<NRHMDPoseTracker>();

            if (NRHMDPoseTracker == null)
            {
                NRDebugger.Error("[NRSessionManager] Can not find the NRHMDPoseTracker in the NRSessionBehaviour object.");
                OprateInitException(new NRMissingKeyComponentError("Missing the key component of 'NRHMDPoseTracker'."));
                return;
            }

            try
            {
                NRDevice.Instance.Init();
            }
            catch (Exception)
            {
                return;
            }

            NativeAPI = new NativeInterface();
            AsyncTaskExecuter.Instance.RunAction(() =>
            {
                NRDebugger.Debug("[NRSessionManager] Create tracking");
                switch (NRHMDPoseTracker.TrackingMode)
                {
                    case NRHMDPoseTracker.TrackingType.Tracking6Dof:
                        NativeAPI.NativeTracking.Create();
                        NativeAPI.NativeTracking.SetTrackingMode(TrackingMode.MODE_6DOF);
                        break;
                    case NRHMDPoseTracker.TrackingType.Tracking3Dof:
                        NativeAPI.NativeTracking.Create();
                        NRSessionBehaviour.SessionConfig.PlaneFindingMode = TrackablePlaneFindingMode.DISABLE;
                        NRSessionBehaviour.SessionConfig.ImageTrackingMode = TrackableImageFindingMode.DISABLE;
                        NativeAPI.NativeTracking.SetTrackingMode(TrackingMode.MODE_3DOF);
                        break;
                    default:
                        break;
                }
            });

            TrackingLostListener = new NRTrackingModeChangedListener();
#if USING_XR_SDK && !UNITY_EDITOR
            TrackingLostListener.OnTrackStateChanged += (bool onSwitchingMode, RenderTexture rt) =>
            {
                SetSwitchModeFrameInfo(new SwitchModeFrameInfo() { flag = onSwitchingMode, renderTexture = rt.GetNativeTexturePtr() });
            };
#endif
            NRKernalUpdater.OnPreUpdate -= OnPreUpdate;
            NRKernalUpdater.OnPreUpdate += OnPreUpdate;
            SessionState = SessionState.Initialized;

            LoadNotification();
        }

        /// <summary> Loads the notification. </summary>
        private void LoadNotification()
        {
            if (GameObject.FindObjectOfType<NRNotificationListener>() == null)
            {
                GameObject.Instantiate(Resources.Load("NRNotificationListener"));
            }
        }

        /// <summary> True if is session error, false if not. </summary>
        private bool m_IsSessionError = false;
        /// <summary> Oprate initialize exception. </summary>
        /// <param name="e"> An Exception to process.</param>
        internal void OprateInitException(Exception e)
        {
            NRDebugger.Error("[NRSessionManager] Kernal error:" + e.Message);
            if (m_IsSessionError || !(e is NRKernalError))
            {
                return;
            }

            var kernal_error = e as NRKernalError;
            if (kernal_error.level == Level.High)
            {
                m_IsSessionError = true;
                ShowErrorTips(kernal_error);
            }
            else if (kernal_error.level == Level.Normal)
            {
                OnKernalError?.Invoke((NRKernalError)e);
            }
        }

        private string GetErrorMsg(Exception error)
        {
            if (error is NRGlassesConnectError)
            {
                return NativeConstants.GlassesDisconnectErrorTip;
            }
            else if (error is NRSdkVersionMismatchError)
            {
                return NativeConstants.SdkVersionMismatchErrorTip;
            }
            else if (error is NRSdcardPermissionDenyError)
            {
                return NativeConstants.SdcardPermissionDenyErrorTip;
            }
            else
            {
                return NativeConstants.UnknowErrorTip;
            }
        }

        /// <summary> Shows the error tips. </summary>
        /// <param name="msg"> The message.</param>
        private void ShowErrorTips(NRKernalError error)
        {
            string msg = GetErrorMsg(error);

            var sessionbehaviour = GameObject.FindObjectOfType<NRSessionBehaviour>();
            NRGlassesInitErrorTip errortips;
            if (sessionbehaviour != null && sessionbehaviour.SessionConfig.GlassesErrorTipPrefab != null)
            {
                errortips = GameObject.Instantiate<NRGlassesInitErrorTip>(sessionbehaviour.SessionConfig.GlassesErrorTipPrefab);
            }
            else
            {
                errortips = GameObject.Instantiate<NRGlassesInitErrorTip>(Resources.Load<NRGlassesInitErrorTip>("NRErrorTips"));
            }

            // Clear objects of scene.
            if (sessionbehaviour != null)
            {
                GameObject.Destroy(sessionbehaviour.gameObject);
            }
            var input = GameObject.FindObjectOfType<NRInput>();
            if (input != null)
            {
                GameObject.Destroy(input.gameObject);
            }
            var virtualdisplay = GameObject.FindObjectOfType<NRVirtualDisplayer>();
            if (virtualdisplay != null)
            {
                GameObject.Destroy(virtualdisplay.gameObject);
            }

            GameObject.DontDestroyOnLoad(errortips);
            errortips.Init(msg, () =>
            {
                NRDevice.QuitApp();
            });
        }

        /// <summary> Executes the 'pre update' action. </summary>
        private void OnPreUpdate()
        {
            if (SessionState != SessionState.Running || m_IsSessionError)
            {
                return;
            }

#if USING_XR_SDK && !UNITY_EDITOR
            m_LostTrackingReason = GetLostTrackingReason();
#else
            m_LostTrackingReason = NativeAPI.NativeHeadTracking.GetTrackingLostReason();
            NRFrame.OnUpdate();
#endif
        }

        /// <summary> Sets a configuration. </summary>
        /// <param name="config"> The configuration.</param>
        public void SetConfiguration(NRSessionConfig config)
        {
            if (SessionState == SessionState.UnInitialized
                || SessionState == SessionState.Destroyed
                || SessionState == SessionState.Paused
                || m_IsSessionError)
            {
                NRDebugger.Error("[NRSessionManager] Can not set configuration in this state:" + SessionState.ToString());
                return;
            }
#if !UNITY_EDITOR
            if (config == null)
            {
                return;
            }
            AsyncTaskExecuter.Instance.RunAction(() =>
            {
                NRDebugger.Info("[NRSessionManager] Update config");
                NativeAPI.Configration.UpdateConfig(config);
            });
#endif
        }

        /// <summary> Recenters this object. </summary>
        public void Recenter()
        {
            if (SessionState != SessionState.Running || m_IsSessionError)
            {
                return;
            }
            AsyncTaskExecuter.Instance.RunAction(() =>
            {
                NativeAPI.NativeTracking.Recenter();
            });
        }

        /// <summary> Sets application settings. </summary>
        private void SetAppSettings()
        {
            Application.targetFrameRate = 240;
            QualitySettings.maxQueuedFrames = -1;
            QualitySettings.vSyncCount = 0;
            Screen.fullScreen = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        /// <summary> Starts a session. </summary>
        public void StartSession()
        {
            if (SessionState == SessionState.Running
                || SessionState == SessionState.Destroyed
                || m_IsSessionError)
            {
                return;
            }

            AfterInitialized(() =>
            {
                NRDebugger.Debug("[NRSessionManager] Start session after initialized");
#if !UNITY_EDITOR && !USING_XR_SDK
                if (NRSessionBehaviour.gameObject.GetComponent<NRRenderer>() == null)
                {
                    NRRenderer = NRSessionBehaviour.gameObject.AddComponent<NRRenderer>();
                    NRRenderer.Initialize(NRHMDPoseTracker.leftCamera, NRHMDPoseTracker.rightCamera);
                }
#endif

#if USING_XR_SDK && !UNITY_EDITOR
                XRDisplaySubsystem?.Start();
                XRInputSubsystem?.Start();
#endif

                AsyncTaskExecuter.Instance.RunAction(() =>
                {
                    NRDebugger.Debug("[NRSessionManager] Start tracking");
                    NativeAPI.NativeTracking.Start();
                    NativeAPI.NativeHeadTracking.Create();
                    SessionState = SessionState.Running;
                });

#if UNITY_EDITOR
                InitEmulator();
#endif
                SetConfiguration(NRSessionBehaviour.SessionConfig);
            });
        }

        /// <summary>Do it after session manager initialized. </summary>
        /// <param name="callback"> The after initialized callback.</param>
        private void AfterInitialized(Action callback)
        {
            NRKernalUpdater.Instance.StartCoroutine(WaitForInitialized(callback));
        }

        /// <summary> Wait for initialized. </summary>
        /// <param name="affterInitialized"> The affter initialized.</param>
        /// <returns> An IEnumerator. </returns>
        private IEnumerator WaitForInitialized(Action affterInitialized)
        {
            while (SessionState == SessionState.UnInitialized || m_IsSessionError)
            {
                NRDebugger.Debug("[NRSessionManager] Wait for initialized...");
                yield return new WaitForEndOfFrame();
            }
            affterInitialized?.Invoke();
        }

        /// <summary> Disables the session. </summary>
        public void DisableSession()
        {
            if (SessionState != SessionState.Running || m_IsSessionError)
            {
                return;
            }

            // Do not put it in other thread...
            NRRenderer?.Pause();
            NativeAPI.NativeTracking?.Pause();
            NRDevice.Instance.Pause();
#if USING_XR_SDK && !UNITY_EDITOR
            XRDisplaySubsystem?.Stop();
            XRInputSubsystem?.Stop();
#endif
            SessionState = SessionState.Paused;
        }

        /// <summary> Resume session. </summary>
        public void ResumeSession()
        {
            if (SessionState != SessionState.Paused || m_IsSessionError)
            {
                return;
            }

            // Do not put it in other thread...
            NativeAPI.NativeTracking.Resume();
            NRRenderer?.Resume();
            NRDevice.Instance.Resume();
#if USING_XR_SDK && !UNITY_EDITOR
            XRDisplaySubsystem?.Start();
            XRInputSubsystem?.Start();
#endif
            SessionState = SessionState.Running;
        }

        /// <summary> Destroys the session. </summary>
        public void DestroySession()
        {
            if (SessionState == SessionState.Destroyed || SessionState == SessionState.UnInitialized)
            {
                return;
            }

            // Do not put it in other thread...
            SessionState = SessionState.Destroyed;
            NRRenderer?.Destroy();
            NativeAPI.NativeHeadTracking.Destroy();
            NativeAPI.NativeTracking.Destroy();
            NRDevice.Instance.Destroy();
            NRInput.Destroy();
            VirtualDisplayer.Destory();

#if USING_XR_SDK && !UNITY_EDITOR
            ShutDownNativeSystems();
#endif
        }

        public void SetFPSMode(FrameRateMode mode)
        {
#if USING_XR_SDK && !UNITY_EDITOR
            SetFrameRateMode(mode);
#endif
        }

#if USING_XR_SDK && !UNITY_EDITOR
        [DllImport("NrealXRPlugin", CharSet = CharSet.Auto)]
        static extern void ShutDownNativeSystems();

        [DllImport("NrealXRPlugin", CharSet = CharSet.Auto)]
        static extern void SetFrameRateMode(FrameRateMode mode);

        [DllImport("NrealXRPlugin", CharSet = CharSet.Auto)]
        static extern void SetSwitchModeFrameInfo(SwitchModeFrameInfo mode);

        [DllImport("NrealXRPlugin", CharSet = CharSet.Auto)]
        static extern LostTrackingReason GetLostTrackingReason();
#endif

        /// <summary> Initializes the emulator. </summary>
        private void InitEmulator()
        {
            if (!NREmulatorManager.Inited && !GameObject.Find("NREmulatorManager"))
            {
                NREmulatorManager.Inited = true;
                GameObject.Instantiate(Resources.Load("Prefabs/NREmulatorManager"));
            }
            if (!GameObject.Find("NREmulatorHeadPos"))
            {
                GameObject.Instantiate(Resources.Load("Prefabs/NREmulatorHeadPose"));
            }
        }
    }
}
