﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using System.Windows.Forms;
using Utilities;

namespace SoundboardRandomizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            mediaElement1.LoadedBehavior = MediaState.Manual;
        }

        private GlobalKeyboardHook gKeyboardHook = new GlobalKeyboardHook();

        // check for special functions
        System.Windows.Forms.Keys stopHK;
        System.Windows.Forms.Keys toggleMuteHK;

        // 1 < gHistForgetNum < gMinFileHistCount - 2
        private int gMinFileHistCount = 8;
        private int gHistForgetNum = 5;

        private bool muted = false;

        private int stopKey = 0;
        private int muteToggleKey = 0;

        public struct SoundboardPlaylist
        {
            public List<string> playlist { get; set; }
            public List<int> lastPlayedIdxList { get; set; }
            public int hotkey { get; set; }
        }

        List<SoundboardPlaylist> mSoundboards = new List<SoundboardPlaylist>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            gKeyboardHook.KeyDown += new KeyEventHandler(gKeyboardHook_KeyDown);

            // iterate over dir for files
            LoadPlaylists();
            LoadSpecialHotkeys();
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
                string[] lineSplit = line.Split('-');

                if ((lineSplit.Length == 3) && (lineSplit[0] == "HKSR"))
                {
                    if (lineSplit[2] == "Stop")
                    {
                        stopKey = Int32.Parse(lineSplit[1]);
                    }

                    if (lineSplit[2] == "ToggleMute")
                    {
                        muteToggleKey = Int32.Parse(lineSplit[1]);
                    }
                }
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
            mediaElement1.Play();

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
                mediaElement1.Stop();
                mediaElement1.Volume = 100;
            }
            else
                mediaElement1.Volume = 0;

            muted = !muted;
        }

        private void StopMedia()
        {
            mediaElement1.Stop();
        }

    }
}
