using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Utilities;

// TODO: find out if there's a bug where the soundboard stops working after a while...
// TODO: add some version UI
// TODO: add key to restore all ducking apps to full volume incase they get stuck in the low state

namespace SoundboardRandomizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        // added for 'ducking' other programs volumes
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string strClassName, string strWindowName);

        // added for 'ducking' other programs volumes
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public MainWindow()
        {
            InitializeComponent();
            mediaElement1.LoadedBehavior = MediaState.Manual;
            mediaElement1.MediaEnded += new RoutedEventHandler(RestoreOtherAppsVolumesEventHandler);
        }

        private GlobalKeyboardHook gKeyboardHook = new GlobalKeyboardHook();

        // check for special functions
        System.Windows.Forms.Keys stopHK;
        System.Windows.Forms.Keys toggleMuteHK;

        // 1 < gHistForgetNum < gMinFileHistCount - 2
        private int gMinFileHistCount = 8;
        private int gHistForgetNum = 5;

        private bool muted = false;
        private bool soundAlreadyRunning = false;

        private int stopKey = 0;
        private int muteToggleKey = 0;
        private int sbVolDownKey = 0;
        private int sbVolUpKey = 0;
        private int SpotChrDuckVolDownKey = 0;
        private int SpotChrDuckVolUpKey = 0;

        private int sbVolume = 100;
        private int duckVolume = 40;

        private Dictionary<string, Nullable<Single>> duckingApps = new Dictionary<string, Nullable<Single>>();

        public struct SoundboardPlaylist
        {
            public List<string> playlist { get; set; }
            public List<int> lastPlayedIdxList { get; set; }
            public int hotkey { get; set; }
        }

        private List<SoundboardPlaylist> mSoundboards = new List<SoundboardPlaylist>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            gKeyboardHook.KeyDown += new KeyEventHandler(gKeyboardHook_KeyDown);

            // iterate over dir for files
            LoadPlaylists();

            // Load settings files
            LoadSpecialHotkeys();
            LoadDefaultVolumes();
            LoadVolumeDuckApps();

            AddKeyHooks();

            GC.KeepAlive(gKeyboardHook);
        }

        private void LoadSpecialHotkeys()
        {
            string filePath = System.IO.Directory.GetCurrentDirectory() + "\\HKSR-special-keys.txt";

            if (!System.IO.File.Exists(filePath))
                return;

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(filePath);

            while ((line = file.ReadLine()) != null)
            {
                if (line[0] == '#')
                    continue;

                string[] lineSplit = line.Split('-');

                if ((lineSplit.Length == 3) && (lineSplit[0] == "HKSR"))
                {
                    if (lineSplit[2] == "Stop")
                    {
                        stopKey = Int32.Parse(lineSplit[1]);
                    }
                    else if (lineSplit[2] == "ToggleMute")
                    {
                        muteToggleKey = Int32.Parse(lineSplit[1]);
                    }
                    else if (lineSplit[2] == "SbVolDown")
                    {
                        sbVolDownKey = Int32.Parse(lineSplit[1]);  // TODO: unimplemented
                    }
                    else if(lineSplit[2] == "SbVolUp")
                    {
                        sbVolUpKey = Int32.Parse(lineSplit[1]);  // TODO: unimplemented
                    }
                    else if(lineSplit[2] == "SpotChrDuckVolDown")
                    {
                        SpotChrDuckVolDownKey = Int32.Parse(lineSplit[1]); // TODO: unimplemented
                    }
                    else if(lineSplit[2] == "SpotChrDuckVolUp")
                    {
                        SpotChrDuckVolUpKey = Int32.Parse(lineSplit[1]);  // TODO: unimplemented
                    }
                }
            }
        }

        private void LoadDefaultVolumes()
        {
            string filePath = System.IO.Directory.GetCurrentDirectory() + "\\HKSR-volume-defaults.txt";

            if (!System.IO.File.Exists(filePath))
                return;

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(filePath);

            while ((line = file.ReadLine()) != null)
            {
                if (line[0] == '#')
                    continue;

                string[] lineSplit = line.Split('=');

                if ((lineSplit.Length == 2))
                {
                    if (lineSplit[0] == "sbVolume")
                    {
                        sbVolume = Int32.Parse(lineSplit[1]);
                        mediaElement1.Volume = sbVolume;
                    }
                    else if (lineSplit[0] == "duckVolume")
                    {
                        duckVolume = Int32.Parse(lineSplit[1]);
                    }
                }
            }
        }

        private void LoadVolumeDuckApps()
        {
            string filePath = System.IO.Directory.GetCurrentDirectory() + "\\HKSR-ducking-apps.txt";

            if (!System.IO.File.Exists(filePath))
                return;

            string line;

            System.IO.StreamReader file = new System.IO.StreamReader(filePath);

            while ((line = file.ReadLine()) != null)
            {
                if (line[0] == '#')
                    continue;

                duckingApps.Add(line, 100f);
            }
        }

        private void LoadPlaylists()
        {
            List<string> dirs = new List<string>(System.IO.Directory.EnumerateDirectories(System.IO.Directory.GetCurrentDirectory()));

            foreach (var dir in dirs)
            {
                string[] dirSplit = dir.Split('\\');
                string localDir = dirSplit[dirSplit.Length - 1];

                string[] localDirSplit = localDir.Split('-');

                if ((localDirSplit.Length == 3) && (localDirSplit[0] == "HKSR"))
                {
                    SoundboardPlaylist newSoundboard = new SoundboardPlaylist();

                    newSoundboard.playlist = new List<string>();

                    newSoundboard.playlist.AddRange(System.IO.Directory.GetFiles(dir, "*.mp3"));
                    newSoundboard.playlist.AddRange(System.IO.Directory.GetFiles(dir, "*.wav"));

                    // initialize last played to zero
                    newSoundboard.lastPlayedIdxList = new List<int>();
                    newSoundboard.lastPlayedIdxList.Add(0);
                    
                    // set hotkey enum
                    newSoundboard.hotkey = Int32.Parse(localDirSplit[1]);

                    mSoundboards.Add(newSoundboard);
                }

            }
        }

        private void AddKeyHooks()
        {
            foreach (SoundboardPlaylist soundboard in mSoundboards)
            {
                System.Windows.Forms.Keys playlistHotkey = (System.Windows.Forms.Keys)soundboard.hotkey;
                gKeyboardHook.HookedKeys.Add(playlistHotkey);
            }

            stopHK = (System.Windows.Forms.Keys)stopKey;
            if (stopHK != Keys.None)
                gKeyboardHook.HookedKeys.Add(stopHK);

            toggleMuteHK = (System.Windows.Forms.Keys)muteToggleKey;
            if (toggleMuteHK != Keys.None)
                gKeyboardHook.HookedKeys.Add(toggleMuteHK);

        }

        void gKeyboardHook_KeyDown(object sender, KeyEventArgs e)
        {
            // if this is true it blocks that key from use for everything else
            e.Handled = false;

            if (e.KeyCode == toggleMuteHK)
            {
                ToggleMute();
                return;
            }

            if (e.KeyCode == stopHK)
            {
                StopMedia();
                return;
            }

            if (muted)
                return;

            int sbInd = 0;

            for (; sbInd < mSoundboards.Count; ++sbInd)
            {
                if (e.KeyCode == (System.Windows.Forms.Keys)mSoundboards[sbInd].hotkey)
                    break;

                // got to the end of the soundboards and couldn't find the
                // the registered hotkey, we shouldn't hit this
                if (sbInd == (mSoundboards.Count - 1))
                    return;
            }

            int randomIdx = mSoundboards[sbInd].lastPlayedIdxList[0];

            if (mSoundboards[sbInd].playlist.Count > 1)
            {
                Random r = new Random();

                while (mSoundboards[sbInd].lastPlayedIdxList.Contains(randomIdx))
                    randomIdx = r.Next(0, mSoundboards[sbInd].playlist.Count);

                if (mSoundboards[sbInd].playlist.Count >= gMinFileHistCount)
                {
                    // leave 2 out to choose from next time so it's not cyclic
                    if (mSoundboards[sbInd].lastPlayedIdxList.Count == (mSoundboards[sbInd].playlist.Count - gHistForgetNum))
                    {
                        List<int> tempList = new List<int>();
                        tempList = mSoundboards[sbInd].lastPlayedIdxList.GetRange(1, (mSoundboards[sbInd].lastPlayedIdxList.Count - 1));
                        mSoundboards[sbInd].lastPlayedIdxList.Clear();
                        mSoundboards[sbInd].lastPlayedIdxList.AddRange(tempList);
                    }

                    mSoundboards[sbInd].lastPlayedIdxList.Add(randomIdx);
                }
                else
                    mSoundboards[sbInd].lastPlayedIdxList[0] = randomIdx;
            }

            mediaElement1.Source = new System.Uri(mSoundboards[sbInd].playlist[randomIdx]);

            DuckOtherAppsVolumes();
            mediaElement1.Play();
            // there is a event handlers to RestoreOtherAppsVolumes, we don't use it to Duck 
            // since we can override sounds so they never finish, we just want to run the Duck once else we'll overide
            // current volume levels with ducked volume levels

            // I don't like this but it works
            SoundboardPlaylist newSoundboard = new SoundboardPlaylist
            {
                playlist = mSoundboards[sbInd].playlist,
                hotkey = mSoundboards[sbInd].hotkey,
                lastPlayedIdxList = mSoundboards[sbInd].lastPlayedIdxList
            };

            mSoundboards[sbInd] = newSoundboard;
            return;
        }

        private void ToggleMute()
        {
            if (muted)
            {
                StopMedia();
                mediaElement1.Volume = sbVolume;
                mediaElement1.IsMuted = false;
            }
            else
            {
                mediaElement1.Volume = 0;
                mediaElement1.IsMuted = true;
                StopMedia();
            }

            muted = !muted;
        }

        private void StopMedia()
        {
            mediaElement1.Stop();
            RestoreOtherAppsVolumes();
        }

        private void DuckOtherAppsVolumes()
        {
            if (soundAlreadyRunning)
                return;

            soundAlreadyRunning = true;

            if (duckingApps.Count == 0)
                return;

            foreach (var process in Process.GetProcesses())
            {
                if (duckingApps.ContainsKey(process.ProcessName) /*&& !String.IsNullOrEmpty(process.MainWindowTitle)*/)
                {
                    Nullable<Single> currentVolume = VolumeMixer.GetApplicationVolume(process.Id);
                    if (currentVolume != null)
                        duckingApps[process.ProcessName] = currentVolume;

                    VolumeMixer.SetApplicationVolume(process.Id, Convert.ToSingle(duckVolume));
                }
            }
        }

        private void RestoreOtherAppsVolumesEventHandler(object sender, RoutedEventArgs e)
        {
            RestoreOtherAppsVolumes();
            // this is hacky, but we do this so we can have it on the event handler and also call it independently from StopMedia
        }

        private void RestoreOtherAppsVolumes()
        {
            soundAlreadyRunning = false;

            if (duckingApps.Count == 0)
                return;

            foreach (var process in Process.GetProcesses())
                if (duckingApps.ContainsKey(process.ProcessName) /*&& !String.IsNullOrEmpty(process.MainWindowTitle)*/)
                    VolumeMixer.SetApplicationVolume(process.Id, Convert.ToSingle(duckingApps[process.ProcessName]));
        }

    }

    // added for 'ducking' other programs volumes
    public class VolumeMixer
    {
        public static float? GetApplicationVolume(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            float level;
            volume.GetMasterVolume(out level);
            Marshal.ReleaseComObject(volume);
            return level * 100;
        }

        public static bool? GetApplicationMute(int pid)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return null;

            bool mute;
            volume.GetMute(out mute);
            Marshal.ReleaseComObject(volume);
            return mute;
        }

        public static void SetApplicationVolume(int pid, float level)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMasterVolume(level / 100, ref guid);
            Marshal.ReleaseComObject(volume);
        }

        public static void SetApplicationMute(int pid, bool mute)
        {
            ISimpleAudioVolume volume = GetVolumeObject(pid);
            if (volume == null)
                return;

            Guid guid = Guid.Empty;
            volume.SetMute(mute, ref guid);
            Marshal.ReleaseComObject(volume);
        }

        private static ISimpleAudioVolume GetVolumeObject(int pid)
        {
            // get the speakers (1st render + multimedia) device
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            // activate the session manager. we need the enumerator
            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            // enumerate sessions for on this device
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);
            int count;
            sessionEnumerator.GetCount(out count);

            // search for an audio session with the required name
            // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
            ISimpleAudioVolume volumeControl = null;
            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl2 ctl;
                sessionEnumerator.GetSession(i, out ctl);
                int cpid;
                ctl.GetProcessId(out cpid);

                if (cpid == pid)
                {
                    volumeControl = ctl as ISimpleAudioVolume;
                    break;
                }
                Marshal.ReleaseComObject(ctl);
            }
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            return volumeControl;
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        // the rest is not implemented
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl2 Session);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig]
        int NotImpl0();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int NotImpl1();

        [PreserveSig]
        int NotImpl2();

        // IAudioSessionControl2
        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetProcessId(out int pRetVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }
}
