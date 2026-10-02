# Mobile Driving Simulator

A data-driven Unity 6 LTS/URP starter game for Android and iOS. It includes a forgiving WheelCollider vehicle, chase/hood cameras, fuel and refuelling, three mission modes, economy/progression, a garage/shop, JSON saves, mobile controls, HUD/menu presenters, pooling hooks, and quality presets.

## Build plan

1. Vehicle foundation: car data, WheelCollider controller, automatic gears, anti-roll, engine audio, wheel visuals, and chase/hood camera.
2. Fuel: load-dependent consumption, low-fuel/empty events, paid trigger-based stations.
3. Persistence: versioned JSON profile with atomic writes and autosave lifecycle hooks.
4. Missions and economy: ScriptableObject definitions, runtime objectives, markers, rewards, unlocks.
5. Garage/shop: purchase/select vehicles, upgrade levels, paint, preview turntable, stat comparisons.
6. UI and mobile input: steering wheel/buttons, optional tilt, HUD, mission screens, menus/settings.
7. World/bootstrap and optimization: generated test loop, stations/checkpoints, quality presets, pooling.

Each stage is kept in its own namespace and folder so it can be tested independently. The final section below lists scene wiring and smoke tests.

## Folder structure

```text
Assets/DrivingSim/
├── Art/                 placeholder/replacement art
├── Audio/               replacement audio
├── Editor/              one-click content/scene generator
├── Materials/           generated URP materials and pipeline assets
├── Prefabs/             generated car prefabs
├── Scenes/              generated Demo and Garage scenes
├── ScriptableObjects/   database, 3 cars, 3 missions, 5 upgrades
├── Scripts/
│   ├── Camera/          chase/hood camera
│   ├── Core/            bootstrap, database, wallet contract, pooling, quality
│   ├── Economy/         persistent currency service
│   ├── Fuel/            consumption and gas stations
│   ├── Garage/          purchases, preview, upgrades and stats
│   ├── Missions/        definitions, objectives, targets and marker
│   ├── Save/            versioned JSON profile
│   ├── UI/              HUD, controls, mission/garage/menu/settings views
│   ├── Vehicles/        WheelCollider drivetrain and vehicle data
│   └── World/           traffic/obstacle extension hooks
└── UI/                  replacement UI art/prefabs
```

## Quick setup

1. Install Unity `6000.0.75f1` (Unity 6 LTS) with Android Build Support and/or iOS Build Support.
2. Open this repository as a Unity project and allow Package Manager to install URP and uGUI.
3. Choose **Driving Sim > Create Demo Content** if the generated scenes are missing.
4. Choose **Driving Sim > Apply Realistic Free Art**. This also runs automatically once after Unity finishes importing and Play Mode is stopped.
5. Open the generated Demo scene and press Play. Keyboard fallback: WASD/arrows, Space handbrake, C camera, Esc pause. Mobile controls are present in the generated canvas.

The demo uses a PBR concept car and a curated CC0 downtown environment. Hidden primitive car bodies and road cubes remain as lightweight physics/collision proxies, so gameplay does not depend on presentation meshes.

The generator is idempotent: run it again after changing example data or after URP finishes importing. Existing generated data is updated rather than duplicated.

## Free visual assets

- **Car Concept** from Khronos glTF Sample Assets, model and textures by Eric Chadwick / Darmstadt Graphics Group GmbH, CC BY 4.0. Three paint overrides are generated without changing the source model.
- **Downtown City MegaKit Standard** by Quaternius, CC0 1.0. The project includes a mobile-focused subset: three complete buildings, streets, markings, props, and PBR textures.
- **Unity glTFast 6.14.1** is embedded under `Packages/` to make the car import reproducible and offline-friendly.

Original notices are preserved beside each source asset and summarized in `THIRD_PARTY_NOTICES.md`.

## Build

### Android

Install the Android module from Unity Hub. In **File > Build Profiles**, add Android, switch platform, use IL2CPP, ARM64, landscape orientation, and add `Demo.unity`. Set a package identifier such as `com.yourstudio.drivingsim`, then build an APK/AAB. Medium quality is the default target.

### iOS

Install the iOS module (the final Xcode build requires macOS). Switch to iOS in **Build Profiles**, use IL2CPP/ARM64, add `Demo.unity`, set the bundle identifier and signing team, then export the Xcode project.

## Scene wiring (manual alternative)

- **Car root**: `Rigidbody`, body collider, `CarController`, `FuelSystem`, `EngineAudio`, and four child `WheelCollider`s. Assign corresponding visual wheel transforms. Put the root on a `Vehicle` layer if desired.
- **Camera**: add `DrivingCameraController`; assign the car root plus chase and hood anchors.
- **Systems**: one persistent `GameBootstrap` object creates/owns `SaveService`, `EconomyService`, and `MissionManager`; assign a `GameDatabase` containing all car, upgrade, and mission definitions.
- **HUD canvas**: `HudController` with speed/fuel/money labels, fuel fill, low-fuel warning, objective label and direction arrow. Mobile input widgets write to `MobileInputState`.
- **Gas station**: trigger collider plus `GasStation`; set price per litre. The player can refuel while inside by holding the UI refuel button.
- **Mission marker**: `MissionWaypointMarker` reads the active objective and repositions itself. A mission list button calls `MissionManager.StartMission`.
- **Garage**: `GarageController`, `GarageView`, a preview camera, light, and `PreviewTurntable`; shop buttons call the view methods.

## Smoke tests

- Drive forward/reverse, steer at low/high speed, brake and handbrake; confirm wheel meshes and engine pitch follow physics.
- Empty fuel (or lower consumption test values) and confirm torque cuts out; refuel only when the profile has money.
- Complete each example objective and verify reward/unlock plus a JSON file at `Application.persistentDataPath/driving-save.json`.
- Buy/select a car, buy each upgrade, change paint, restart Play Mode, and confirm the profile reloads.
- Toggle touch/tilt controls, camera mode, pause, audio, and Low/Medium/High quality.

## Performance targets

The generated demo uses baked-compatible static geometry, conservative shadow distance, SRP Batcher-compatible materials, no per-frame allocations in vehicle physics, and pooled world markers. For production: bake lighting/occlusion, atlas textures, use ASTC on mobile, add LODGroups to replacement art, pool traffic/effects, and profile on device before increasing traffic density.

## Validation note

All runtime and editor C# sources were compiled with the C# compiler bundled in Unity `6000.0.75f1`, referencing that editor's Unity 6 assemblies. Serialized-field warnings in this external check are expected because Unity/editor-generated scene references are assigned through serialization. A full Unity import and Play Mode smoke test still requires an activated Unity Editor license on the machine opening the project.
