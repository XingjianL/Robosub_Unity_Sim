# Robosub Unity Sim

This is a Unity Project with a focus on synthetic data generation for tasks in the RoboSub Competition. It is completed, I am making a newer one using HDRP here [https://github.com/XingjianL/RoboSubSim](https://github.com/XingjianL/RoboSubSim), which will provide better water effects and with plans for robot simulation.

- Capable of auto-labeling bounding boxes for YOLOv5 training.
- Domain Randomization (Scene Lighting and Depth, Camera Location and Rotation, Game Object Randomization)
- Not all bounding boxes are accurate (if obstructed it would still label, but it is fairly unlikely so usually it's fine to ignore)

## Usage

1. Install Unity Hub
2. Clone this Repo
3. In Unity Hub's Projects tab, click Open -> Add project from disk -> select this repo folder
4. Get `2021.3.17f1` for the editor version (newer versions might work but idk)
5. Click to start the project in Unity Hub
6. The scene is named `SampleScene` under the Scenes folder in the editor
7. The majority of the control code is under `main_camera_2023.cs` which is attached to the `Main Camera` object

## Scene
![sample image](/example.png)

## Generated Images w/ BBox for YOLOv5 training
### Gate
![sample image](/gate.jpg)
### Buoy
![sample image](/buoy.jpg)
### Bins
![sample image](/bins.jpg)
