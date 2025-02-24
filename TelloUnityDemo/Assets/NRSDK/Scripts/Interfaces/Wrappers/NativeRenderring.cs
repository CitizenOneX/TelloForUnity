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
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    /// <summary>
    /// HMD Eye offset Native API .
    /// </summary>
    public class NativeRenderring
    {
        private UInt64 m_RenderingHandle = 0;
        public IntPtr FrameInfoPtr;
        private IntPtr m_FrameTexturesPtr;

        public struct TexturesArray
        {
            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr leftTex;
            [MarshalAs(UnmanagedType.SysInt)]
            public IntPtr rightTex;
        }

        public NativeRenderring()
        {
            int sizeOfPerson = Marshal.SizeOf(typeof(FrameInfo));
            FrameInfoPtr = Marshal.AllocHGlobal(sizeOfPerson);

            int sizeOfTextures = Marshal.SizeOf(typeof(TexturesArray));
            m_FrameTexturesPtr = Marshal.AllocHGlobal(sizeOfTextures);
        }

        ~NativeRenderring()
        {
            Marshal.FreeHGlobal(FrameInfoPtr);
            Marshal.FreeHGlobal(m_FrameTexturesPtr);
        }

        public bool Create()
        {
            var result = NativeApi.NRRenderingCreate(ref m_RenderingHandle);
            //AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            //AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            //AndroidJavaObject unityPlayerObj = activity.Get<AndroidJavaObject>("mUnityPlayer");
            //AndroidJavaObject surfaceViewObj = unityPlayerObj.Call<AndroidJavaObject>("getChildAt", 0);
            //AndroidJavaObject surfaceHolderObj = surfaceViewObj.Call<AndroidJavaObject>("getHolder");
            //AndroidJavaObject surfaceObj = surfaceHolderObj.Call<AndroidJavaObject>("getSurface");
            //var result = NativeApi.NRRenderingInitSetAndroidSurface(m_RenderingHandle, surfaceObj.GetRawObject());
            NativeErrorListener.Check(result, this, "Create");

#if !UNITY_STANDALONE_WIN
            NativeColorSpace colorspace = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
               NativeColorSpace.COLOR_SPACE_GAMMA : NativeColorSpace.COLOR_SPACE_LINEAR;
            NativeApi.NRRenderingInitSetTextureColorSpace(m_RenderingHandle, colorspace);
#endif
            //NativeApi.NRRenderingInitSetFlags();

            return result == NativeResult.Success;
        }

        public bool Start()
        {
            var result = NativeApi.NRRenderingStart(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Start");
            return result == NativeResult.Success;
        }

        public bool Pause()
        {
            var result = NativeApi.NRRenderingPause(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Pause");
            return result == NativeResult.Success;
        }

        public bool Resume()
        {
            var result = NativeApi.NRRenderingResume(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Resume");
            return result == NativeResult.Success;
        }

        public void GetFramePresentTime(ref UInt64 present_time)
        {
            if (m_RenderingHandle == 0)
            {
                return;
            }
            NativeApi.NRRenderingGetFramePresentTime(m_RenderingHandle, ref present_time);
        }

        public bool DoRender()
        {
            FrameInfo framinfo = (FrameInfo)Marshal.PtrToStructure(FrameInfoPtr, typeof(FrameInfo));
#if !UNITY_EDITOR
            var result = NativeApi.NRRenderingDoRender(m_RenderingHandle, framinfo.leftTex, framinfo.rightTex, ref framinfo.headPose);
            return result == NativeResult.Success;
#else
            return true;
#endif
        }

        public void DoExtendedRenderring()
        {
            FrameInfo framinfo = (FrameInfo)Marshal.PtrToStructure(FrameInfoPtr, typeof(FrameInfo));

            UInt64 frame_handle = 0;
            NativeApi.NRFrameCreate(m_RenderingHandle, ref frame_handle);
            NativeApi.NRFrameSetColorTextures(m_RenderingHandle, frame_handle, m_FrameTexturesPtr, 2);
            NativeApi.NRFrameSetRenderingPose(m_RenderingHandle, frame_handle, ref framinfo.headPose);
            NativeApi.NRFrameSetFocusPlanePoint(m_RenderingHandle, frame_handle, ref framinfo.focusPosition);
            NativeApi.NRFrameSetFocusPlaneNormal(m_RenderingHandle, frame_handle, ref framinfo.normalPosition);
            NativeApi.NRFrameSetPresentTime(m_RenderingHandle, frame_handle, framinfo.presentTime);
            NativeApi.NRFrameSetFlag(m_RenderingHandle, frame_handle, (int)(framinfo.changeFlag));
            NativeApi.NRFrameSetColorTextureType(m_RenderingHandle, frame_handle, framinfo.textureType);

            var result = NativeApi.NRRenderingDoRenderEx(m_RenderingHandle, frame_handle);
            NativeErrorListener.Check(result, this, "DoRenderEXP");

            NativeApi.NRFrameDestroy(m_RenderingHandle, frame_handle);
        }

        public void WriteFrameData(FrameInfo frame)
        {
            Marshal.StructureToPtr(frame, FrameInfoPtr, true);
            TexturesArray textures = new TexturesArray()
            {
                leftTex = frame.leftTex,
                rightTex = frame.rightTex
            };
            Marshal.StructureToPtr(textures, m_FrameTexturesPtr, true);
        }

        public bool Destroy()
        {
            NativeResult result = NativeApi.NRRenderingDestroy(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Destroy");
            return result == NativeResult.Success;
        }

        private partial struct NativeApi
        {
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingCreate(ref UInt64 out_rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingInitSetTextureColorSpace(UInt64 rendering_handle, NativeColorSpace color_space);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingStart(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingDestroy(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingPause(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingResume(UInt64 rendering_handle);

#if !UNITY_EDITOR
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingInitSetAndroidSurface(
                UInt64 rendering_handle, IntPtr android_surface);
#endif

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingDoRenderEx(UInt64 rendering_handle, UInt64 frame_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingInitSetFlags(UInt64 rendering_handle, NRRenderingFlags rendering_flags);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameCreate(UInt64 rendering_handle, ref UInt64 out_frame_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameDestroy(UInt64 rendering_handle, UInt64 frame_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetColorTextures(UInt64 rendering_handle, UInt64 frame_handle,
                IntPtr color_textures, int color_texture_count);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetRenderingPose(UInt64 rendering_handle, UInt64 frame_handle,
                ref NativeMat4f transform);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetFocusPlanePoint(UInt64 rendering_handle, UInt64 frame_handle,
                ref NativeVector3f plane_point);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetFocusPlaneNormal(UInt64 rendering_handle, UInt64 frame_handle,
                ref NativeVector3f plane_normal);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetPresentTime(UInt64 rendering_handle, UInt64 frame_handle, UInt64 present_time);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetFlag(UInt64 rendering_handle, UInt64 frame_handle,
                int change_flag);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRFrameSetColorTextureType(UInt64 rendering_handle, UInt64 frame_handle,
                NRTextureType texture_type);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingGetFramePresentTime(UInt64 rendering_handle, ref UInt64 frame_present_time);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingDoRender(UInt64 rendering_handle,
                IntPtr left_eye_texture, IntPtr right_eye_texture, ref NativeMat4f head_pose);
        };
    }
}