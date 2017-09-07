
using System;
using System.Runtime.InteropServices;
using Windows.Media.Core;

namespace ThreeDToolkit
{
    /// <summary>
    /// Lightweight adapter for uwp unity specific rendering component
    /// </summary>
    /// <remarks>
    /// Depends on being able to <c>DllImport</c> ThreeDToolkit.Rendering.UWP
    /// </remarks>
    internal class UnityConductorAdapter : IDisposable
    {
        private bool disposedValue = false;

        public UnityConductorAdapter()
        {
            Rendering.CreateMediaPlayback();
        }

        public void Play(MediaStreamSource source)
        {
            Rendering.LoadMediaStreamSource(source);
            Rendering.Play();
        }

        public void GetTexture(uint width, uint height, out IntPtr texturePtr)
        {
            Rendering.GetPrimaryTexture(width, height, out texturePtr);
        }
        
        #region IDisposable Support
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Rendering.Stop();
                    Rendering.ReleaseMediaPlayback();
                }
                
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        private static class Rendering
        {
            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "CreateMediaPlayback")]
            internal static extern void CreateMediaPlayback();

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "ReleaseMediaPlayback")]
            internal static extern void ReleaseMediaPlayback();

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetPrimaryTexture")]
            internal static extern void GetPrimaryTexture(UInt32 width, UInt32 height, out System.IntPtr playbackTexture);

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LoadContent")]
            internal static extern void LoadContent([MarshalAs(UnmanagedType.BStr)] string sourceURL);

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LoadMediaSource")]
            internal static extern void LoadMediaSource(IMediaSource IMediaSourceHandler);

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LoadMediaStreamSource")]
            internal static extern void LoadMediaStreamSource(MediaStreamSource IMediaSourceHandler);

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "Play")]
            internal static extern void Play();

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "Pause")]
            internal static extern void Pause();

            [DllImport("ThreeDToolkit.Rendering.UWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "Stop")]
            internal static extern void Stop();
        }
    }
}
