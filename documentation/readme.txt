all folders with .mp3s or .wavs need to follow the same format:

HKSR-[KEYENUM]-[anything]

where [KEYNUM] is a number from the key-enums.txt file that corresponds to a hotkey
where [anything] is any string to describe the contents of that folder, excl. '-' 

example folders are:

HKSR-39-steamedHams (Right (arrow key) = 39)
HKSR-82-dreams      (R = 82, case doesn't matter)
HKSR-114-memes      (F3 = 114)

haven't figured out how to do alt+hotkey, or shift+hotkey, or ctrl+hotkey or alt+ctrl+hotkey yet
___________________________________________________________________________________________________________

HKSR-special-keys.txt

this file contains the hotkey settings for Toggling Mute and Stopping audio
by default they are set to F1 and tilde respectively but you can change them by changing the number in either line:

HKSR-192-Stop
HKSR-112-ToggleMute

in that file from. It must follow that same format with those exact names 'Stop' and 'ToggleMute' and the file name
must be 'HKSR-special-keys.txt'

when ToggleMute unmutes it also calls Stop.
___________________________________________________________________________________________________________

HKSR-volume-defaults.txt

sbVolume=100
duckVolume=30

duckVolume is the percentages of full volume, where full volume is your current system volume height. 
e.g if your system volume is 50% then duckVolume, is 50% of %50 of your max volume output (25%)

whereas, sbVolume is the sound board application volume, just like the sound slider in the spotify application.
___________________________________________________________________________________________________________

HKSR-ducking-apps.txt

processes to lower volume on temporarily when playing a sound, the ones I saved in there are:

Spotify
#Discord
#chrome
#firefox
#obs64

the case is important, if you are not sure that it's being ducked. open your windows volume mixer when 
you are messing with this file and see if the volumes of the app you are trying to lower are lowering.
they'll be the names in the 'details' tab of your task manager.

NOTE: I've comented out (#comment) chrome and firefox as they make a process for each tab and can slow down the soundboard randomizer
also commented out OBS studio (obs64) and Discord since I don't know if you wanted those on the duck list as well
__________________________________________________________________________________________________________


