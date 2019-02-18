# Papae-AudioManager

###Version 1.3.1
1. Fixed audio cut issues for one shot sound effects 

###Version 1.3.0
1. Made script a single class
2. Made API neater with standard conventions
3. Removed the SoundAsset class and the SoundEffectTag Monobehaviour class
4. Replaced both with a neater SoundEffect Monobehaviour class
5. Removed the function that changes the background music pich to better implement in future updates
6. Removed redundant code

###Version 1.2.0
1. Added the pitch parameter when playing music and sound effects
2. Can now load audioclip from url
3. Added a ChangeBGMPitch which allows the change of the pitch of the background music over a period of time
4. Fixed destroy bug that occurs when you pause a sound effect

###Version 1.1.0
1. Fixed error that occurs when attaching a mixer group without specifying a parameter name
2. Renamed AudioOptions class to AudioPreferences

###Version 1.0.1
1. Fixed the hanging issue when trying to perform another transition effect on one that is currently performing
2. Corrected typo error and changed SoundEFfectTag to SoundEffectTag
3. Added a no music is being played property to the AudioManager class; meaning you can check if a current background music is being played









 
