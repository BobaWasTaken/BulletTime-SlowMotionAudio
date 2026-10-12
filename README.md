# BulletTime Slow Motion Audio

Separate BepInEx audio addon for **dvize.BulletTime 2.3.0**, built for testing on SPT 4.1.6.

Lowers the playback pitch and speed of active Unity AudioSources while BulletTime is active, then restores original pitches when slow motion ends. Covers gunfire, ambience, voices, music and UI routed through Unity AudioSources. Does not modify the original BulletTime plugin.

## Installation

Keep BulletTime 2.3.0 installed. Place `BulletTimeSlowMotionAudio.dll` in `BepInEx/plugins/BulletTimeSlowMotionAudio/`.

## F12 settings

- **Enabled**: enable the audio addon.
- **Slowdown Strength**: 1 follows BulletTime's speed; 0 leaves audio unchanged.
- **Minimum Pitch**: minimum playback multiplier; default 0.1.

## Build

Install a .NET SDK and the .NET Framework 4.7.2 developer/targeting pack on Windows. Use the BepInEx and Unity assemblies from your own SPT installation. These dependencies are not included in this repository.

```powershell
dotnet build BulletTimeSlowMotionAudio.csproj -c Release -p:SptPath="F:\Spt"
```

Output: `bin/Release/net472/BulletTimeSlowMotionAudio.dll`.

The addon discovers `BulletTime.Sync.BulletTimeState.WorldSlowed` and `CurrentScale` through reflection. Its BepInEx dependency GUID is `com.dvize.bullettime`.

## Implementation and limitations

Scans for new AudioSources every 50 ms of real time during slow motion and adjusts tracked sources in LateUpdate. Tracks original pitches to avoid multiplying its own previous adjustment and restores them on exit, disable or unload.

This uses lowered playback pitch, not pitch-preserving time stretching. New sounds may briefly begin at normal pitch; very short sounds can finish before detection. Non-Unity audio is not covered. Native source-release scheduling is unchanged, so some slowed sound tails may be cut short. Other mods changing pitch continuously can interact with the adjustment.

The v1.0.0 source compiled successfully against available SPT/Unity references. The portable project is supplied for local builds; it has not been verified against your installed assemblies. In-game behavior still needs testing.

## Repository contents

Only this audio addon's source and build project are included. No original BulletTime source, game assemblies, audio assets, or compiled dependencies are included. Choose a repository license before accepting outside contributions.
