# ------------------------------------ libavutil
include $(CLEAR_VARS)

LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := avutil
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib/libavutil.a

include $(PREBUILT_STATIC_LIBRARY)

# ------------------------------------ libavformat
include $(CLEAR_VARS)

LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := avformat
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib/libavformat.a

include $(PREBUILT_STATIC_LIBRARY)

# ------------------------------------ libavcodec
include $(CLEAR_VARS)

LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := avcodec
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib/libavcodec.a

include $(PREBUILT_STATIC_LIBRARY)

# ------------------------------------ libswscale
include $(CLEAR_VARS)

LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := swscale
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib/libswscale.a

include $(PREBUILT_STATIC_LIBRARY)

# ------------------------------------ libswresample
include $(CLEAR_VARS)

LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := swresample
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib/libswresample.a

include $(PREBUILT_STATIC_LIBRARY)

# ------------------------------------ libx264
include $(CLEAR_VARS)

LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libx264
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib/libx264.a

include $(PREBUILT_STATIC_LIBRARY)

# ------------------------------------ libTelloVideoDecoder
include $(CLEAR_VARS)

# override strip command to strip all symbols from output library; no need to ship with those..
# cmd-strip = $(TOOLCHAIN_PREFIX)strip $1

#LOCAL_ARM_MODE  := arm
LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libTelloVideoDecoder

FFMPEG_LIB_PATH := $(LOCAL_PATH)/ffmpeg-android-prebuilt/lib

LOCAL_C_INCLUDES := $(LOCAL_PATH)/ffmpeg-android-prebuilt/include
LOCAL_SRC_FILES := RenderAPI.cpp RenderAPI_OpenGLCoreES.cpp RenderingPlugin.cpp TelloVideoDecoder.cpp
LOCAL_CPPFLAGS	:= -D__ANDROID__ -v

#LOCAL_LDFLAGS 	:= -v
LOCAL_LDFLAGS 	:= -L$(FFMPEG_LIB_PATH) -v

LOCAL_LDLIBS    := \
	-llog -lEGL -lGLESv1_CM -lGLESv2 -lz \
#	-lavformat -lavcodec -lswresample -lswscale -lavutil -lx264
# -Wl,--no-warn-shared-textrel

LOCAL_STATIC_LIBRARIES := avutil avcodec avformat swresample swscale x264

APP_ALLOW_MISSING_DEPS=false

ifeq ($(NDK_DEBUG), 0)
LOCAL_CFLAGS += -DNDEBUG
endif

include $(BUILD_SHARED_LIBRARY)
# $(call import-add-path, $(FFMPEG_LIB_PATH) )
