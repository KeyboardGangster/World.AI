
# W o r l d . A I
![Samples of 1km² each.](https://i.imgur.com/sLv1w3i.png)
Text-to-world using OpenAI and procedural generation techniques.
## Introduction
WorldAI focuses on creating procedurally generated worlds from a simple text-prompt (manual input also supported though!).

Using OpenAI's ChatGPT a text can be transformed into commands that will be used to procedurally generate a world compatible with Unity's Terrain assets.

The idea is to simplify creating worlds for your purposes. This can speed up the process of brainstorming and game-/ software-development.
## World-generation
Create your world with a couple simple steps: Either write your prompt for ChatGPT or select your biomes.
![cedar](https://i.imgur.com/Q5JCssr.png)
![desert](https://i.imgur.com/hl7gpxM.png)
![fir](https://i.imgur.com/aOWsdrs.png)
![swamp](https://i.imgur.com/UYmLTsy.png)
## Athmosphere
Each biome comes with its own (definable) atmosphere. Just set the target to track before playing!
### Day-Night-Cycle
WorldAI comes with its own Day-night-cycle! Just set the duration of a day and adjust the time-of-day-slider. You can also fixate the time.

https://github.com/KeyboardGangster/World.AI/assets/120393465/db3bd04c-18a8-4184-9809-1659af5e629e

### Rain
WorldAI can also switch to rainy weather in-game! Just tick the isRaining checkBox or press "R" when playing.

https://github.com/KeyboardGangster/World.AI/assets/120393465/f743b234-e1dd-4dc7-8a06-21f7f250ee03

### Thunder
Also includes thunder! Currently you need to tick the isStricking checkBox to see the effect.

https://github.com/KeyboardGangster/World.AI/assets/120393465/8c0a7c18-7295-48a5-a05b-a5cb69f46091

## Shapes (not ChatGPT)
You can have your biomes generate in shapes aswell!
![shapes](https://i.imgur.com/zEYMEqn.png)
Simply adjust the SOHeight parameters (Bias, Randomness) in the Default-interface. (Doesn't work with ChatGPT-interface)

## How to get started
Add the WorldGenerator-Interface to your scene (either ChatGPT or Default). It will create everything you need for you.
![enter image description here](https://i.imgur.com/sKdaO1U.png)
Simply fill out the parameters and click the generate-button. Your world will be saved in a folder.
![enter image description here](https://i.imgur.com/FMxEsAx.jpg)
You can use the WorldGeneratorArgs (highlighted in image) in your code to get information about the world (e.g. Biome at location, Water-level, used biomes, ...).

## Notes
### Licensing
Project is licensed under MIT.
All models and textures are CC0 (public domain).

External libraries are licensed under their respective licenses. (MIT, CC0)

See Notice-file for more details.
### Version-support
Unity-Editor version 2021.2 or higher (only HDRP for now!)

### Other miscellaneous
You can delete AthmosphereControl and all _DEFAULT... GameObjects in the scene if you want to handle athmosphere-stuff yourself.

 As of yet only a single terrain (terrainData) can be used for generation. There are steps in code to allow for multiple "chunks" but this code is deprecated and needs fixing to work properly (maybe in a later update).

WorldAI supports differently sized worlds as well. Small, Medium, Large are just example sizes. You will need to change the size in code for now (inside the Interface-classes).
