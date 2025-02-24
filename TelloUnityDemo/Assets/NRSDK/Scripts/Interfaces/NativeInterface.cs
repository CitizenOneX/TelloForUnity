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
#if UNITY_STANDALONE_WIN
    using System.Runtime.InteropServices;
    using UnityEngine;
#endif

    /// <summary> Manage the Total Native API. </summary>
    public class NativeInterface
    {
        /// <summary> Default constructor. </summary>
        public NativeInterface()
        {
            //Add Standalone plugin search path.
#if UNITY_STANDALONE_WIN
            NativeApi.SetDllDirectory(System.IO.Path.Combine(Application.dataPath, "Plugins"));
#endif
            NativeHeadTracking = new NativeHeadTracking(this);
            NativeTracking = new NativeTracking(this);
            NativeTrackableImage = new NativeTrackableImage(this);
            NativePlane = new NativePlane(this);
            NativeTrackable = new NativeTrackable(this);
            TrackableFactory = new NRTrackableManager(this);
            Configration = new NativeConfigration(this);
            NativeRenderring = new NativeRenderring();
        }

        /// <summary> Gets or sets the handle of the tracking. </summary>
        /// <value> The tracking handle. </value>
        public UInt64 TrackingHandle { get; set; }

        /// <summary> Gets or sets the native head tracking. </summary>
        /// <value> The native head tracking. </value>
        public NativeHeadTracking NativeHeadTracking { get; set; }

        /// <summary> Gets or sets the native tracking. </summary>
        /// <value> The native tracking. </value>
        public NativeTracking NativeTracking { get; set; }

        /// <summary> Gets or sets the native trackable image. </summary>
        /// <value> The native trackable image. </value>
        public NativeTrackableImage NativeTrackableImage { get; set; }

        /// <summary> Gets or sets the native plane. </summary>
        /// <value> The native plane. </value>
        public NativePlane NativePlane { get; set; }

        /// <summary> Gets or sets the native trackable. </summary>
        /// <value> The native trackable. </value>
        public NativeTrackable NativeTrackable { get; set; }

        /// <summary> Gets or sets the trackable factory. </summary>
        /// <value> The trackable factory. </value>
        public NRTrackableManager TrackableFactory { get; set; }

        /// <summary> Gets or sets the configration. </summary>
        /// <value> The configration. </value>
        public NativeConfigration Configration { get; set; }

        /// <summary> Gets or sets the configration. </summary>
        /// <value> The configration. </value>
        public NativeRenderring NativeRenderring { get; set; }

        private partial struct NativeApi
        {
#if UNITY_STANDALONE_WIN
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetDllDirectory(string lpPathName);
#endif
        }
    }
}
