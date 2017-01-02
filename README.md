# AudioManager (Papae2D.AudioEngine)
![AudioManager - Papae2D.AudioEngine](https://scontent-lhr3-1.xx.fbcdn.net/v/t1.0-9/15873504_1276004475776287_5532099008590403853_n.jpg?oh=254ed9ac001590d67511b9beaf2b96db&oe=58EC50C7)

**AudioManager** is a Unity package for 2D games. It provides a simple way to manage and control your 2D game’s background music and sound effects.


## Features
- An audio manager component in inspector view 
-	Persistent singleton class call from code (no prefabs needed)
-	Static function calls with callbacks 
-	3 background music transition effects (Swift, Linear Fade & Cross Fade)
-	Control of all sound effects in game without tags
-	Integration with AudioMixerGroups
-	Built-in sound pool for looping or repeating sounds
-	Pool for loading audio assets from resource folder
-	Fully commented code for understanding

## Installation
Import the **AudioManager-Papae2D-AudioEngine.unitypackage** or copy the **Papae2D/AudioEngine/AudioManager** folder with it's contents anywhere into your project folder and you are ready to go.


## Usage
1.  Drag and drop the **AudioManager.prefab** gameobject anywhere in the scene or hierarchy, edit any properties visible in the Inspector then call any API related function or attribute from code

2.  Attach or add the **AudioManager.cs** class as a component to an empty game object in the scene, edit any properties visible in the Inspector then call any API related function or attribute from code

3.  Just fire or call any API related function or attribute from code 

Note that you have to import the namespace **Papae2D.AudioEngine** to use the AudioManager in script


### Fade In and Fade Out
> AudioManager.PlayBGM(clip, MusicTransition.LinearFade, 3f);

### Play a Sound Effect for 10 secs
> AudioManager.PlaySFX(clip, duration);

### Loop or Repeat a Sound at a particular location 5 times
> AudioManager.RepeatSFX(clip, 5, worldLocation);

Read the [API Reference](https://github.com/JackM36/Eazy-Sound-Manager/wiki) for more information.


## Contributors
- Oluwaseye Ayinla (https://github.com/oluwaseyeayinla)


## License
MIT License. Copyright 2016 Oluwaseye Ayinla.
