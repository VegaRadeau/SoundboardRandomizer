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

Also added HKSR-special-keys.txt
this file contains the hotkey settings for Toggling Mute and Stopping audio
by default they are set to F1 and tilde respectively but you can change them by changing the number in either line:

HKSR-192-Stop
HKSR-112-ToggleMute

in that file from. It must follow that same format with those exact names 'Stop' and 'ToggleMute' and the file name
must be 'HKSR-special-keys.txt'

when ToggleMute unmutes it also calls Stop.