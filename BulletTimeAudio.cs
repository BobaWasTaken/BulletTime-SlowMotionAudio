using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
namespace BulletTimeAudioAddon
{
    [BepInPlugin("local.bullettime.audio", "BulletTime Slow Motion Audio", "1.0.0")]
    [BepInDependency("com.dvize.bullettime")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _strength, _minimum;
        private PropertyInfo _worldSlowed, _currentScale;
        private readonly Dictionary<AudioSource, SourceState> _sources = new Dictionary<AudioSource, SourceState>();
        private readonly List<AudioSource> _dead = new List<AudioSource>();
        private float _nextScan;
        private bool _active, _failed;
        private sealed class SourceState { public float BasePitch, AppliedPitch; public bool Applied; }
        private void Awake()
        {
            _enabled = Config.Bind("Audio", "Enabled", true, "Slow Unity audio while BulletTime is active.");
            _strength = Config.Bind("Audio", "Slowdown Strength", 1f, new ConfigDescription("1 follows BulletTime speed exactly; 0 leaves sound unchanged.", new AcceptableValueRange<float>(0f, 1f)));
            _minimum = Config.Bind("Audio", "Minimum Pitch", 0.1f, new ConfigDescription("Lowest playback speed multiplier. Prevents nearly inaudible sound at extreme slow motion.", new AcceptableValueRange<float>(0.05f, 1f)));
            try
            {
                Type state = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    state = assembly.GetType("BulletTime.Sync.BulletTimeState", false);
                    if (state != null) break;
                }
                if (state == null) throw new TypeLoadException("BulletTime 2.3.0 state not found.");
                _worldSlowed = state.GetProperty("WorldSlowed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                _currentScale = state.GetProperty("CurrentScale", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (_worldSlowed == null || _currentScale == null) throw new MissingMemberException("BulletTime state API changed.");
                Logger.LogInfo("Slow-motion audio ready; follows BulletTime 2.3.0 and restores original pitches on exit.");
            }
            catch (Exception error) { _failed = true; Logger.LogError(error); }
        }
        private void LateUpdate()
        {
            if (_failed) return;
            try
            {
                bool slow = _enabled.Value && (bool)_worldSlowed.GetValue(null, null);
                float scale = slow ? (float)_currentScale.GetValue(null, null) : 1f;
                if (float.IsNaN(scale) || float.IsInfinity(scale)) scale = 1f;
                float multiplier = Mathf.Max(_minimum.Value, Mathf.Lerp(1f, Mathf.Clamp01(scale), _strength.Value));
                slow = slow && multiplier < 0.9999f;
                if (!slow)
                {
                    if (_active) { Restore(); Logger.LogInfo("BulletTime audio restored."); }
                    return;
                }
                if (!_active || Time.unscaledTime >= _nextScan)
                {
                    // No global DSP effect: only Unity AudioSources, including pooled gunshot sources.
                    foreach (AudioSource source in UnityEngine.Object.FindObjectsOfType<AudioSource>())
                        if (source != null && !_sources.ContainsKey(source))
                            _sources.Add(source, new SourceState { BasePitch = source.pitch });
                    _nextScan = Time.unscaledTime + 0.05f;
                }
                if (!_active) Logger.LogInfo("BulletTime audio slowed; multiplier=" + multiplier + ", sources=" + _sources.Count);
                _active = true;
                _dead.Clear();
                foreach (var pair in _sources)
                {
                    AudioSource source = pair.Key; SourceState state = pair.Value;
                    if (source == null) { _dead.Add(source); continue; }
                    float current = source.pitch;
                    // Preserve pitch changes made by the game; never multiply our own previous value again.
                    if (!state.Applied || Mathf.Abs(current - state.AppliedPitch) > 0.0001f)
                        state.BasePitch = current;
                    float desired = state.BasePitch * multiplier;
                    if (Mathf.Abs(current - desired) > 0.0001f) source.pitch = desired;
                    state.AppliedPitch = source.pitch;
                    state.Applied = true;
                }
                foreach (AudioSource source in _dead) _sources.Remove(source);
            }
            catch (Exception error)
            {
                Restore(); _failed = true;
                Logger.LogError("Slow-motion audio stopped safely: " + error);
            }
        }
        private void Restore()
        {
            foreach (var pair in _sources)
            {
                AudioSource source = pair.Key; SourceState state = pair.Value;
                if (source != null && state.Applied && Mathf.Abs(source.pitch - state.AppliedPitch) < 0.0001f)
                    source.pitch = state.BasePitch;
            }
            _sources.Clear(); _active = false; _nextScan = 0f;
        }
        private void OnDisable() { Restore(); }
        private void OnDestroy() { Restore(); }
    }
}
