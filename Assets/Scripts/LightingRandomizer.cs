using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using Object = UnityEngine.Object;

[System.Serializable]
public class LightingPreset
{
    [Tooltip("Preset name shown in metrics, e.g. day, night, sunset")]
    public string presetName;
    public Material skybox;
    public float lightIntensity;
    public float lightTemperature;
}

[AddRandomizerMenu("Custom/Lighting Randomizer")]
public class LightingRandomizer : Randomizer
{
    public LightingPreset[] presets;

    // Valori correnti letti dal CustomMetricsLabeler
    public static float[] CurrentLightingValues = new float[] { 0f, 0f, 0f }; // [index, intensity, temperature]
    public static string CurrentPresetName = "";

    protected override void OnIterationStart()
    {
        if (presets == null || presets.Length == 0) return;

        int index = UnityEngine.Random.Range(0, presets.Length);
        LightingPreset preset = presets[index];

        // Apply skybox
        RenderSettings.skybox = preset.skybox;
        DynamicGI.UpdateEnvironment();

        // Apply light
        Light[] lights = Object.FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = preset.lightIntensity;
                light.colorTemperature = preset.lightTemperature;
                light.useColorTemperature = true;
            }
        }

        // Espone i valori correnti per il labeler
        CurrentLightingValues = new float[] { preset.lightIntensity, preset.lightTemperature };
        CurrentPresetName = preset.presetName;
    }
}