using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Perception.Randomization.Randomizers;

[AddRandomizerMenu("Custom/Post Processing Randomizer")]
public class PostProcessingRandomizer : Randomizer
{
    [Header("Color Adjustments")]
    public float contrastMin = -30f;
    public float contrastMax = 30f;
    public float saturationMin = -30f;
    public float saturationMax = 30f;

    [Header("Film Grain")]
    public float grainIntensityMin = 0f;
    public float grainIntensityMax = 0.4f;

    [Header("Vignette")]
    public float vignetteIntensityMin = 0f;
    public float vignetteIntensityMax = 0.4f;

    private Volume volume;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private Vignette vignette;

    protected override void OnAwake()
    {
        volume = GameObject.Find("Global Volume").GetComponent<Volume>();

        // Abilita post processing sulla camera
        var camera = Camera.main;
        var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
            cameraData.renderPostProcessing = true;

        volume.profile.TryGet(out colorAdjustments);
        volume.profile.TryGet(out filmGrain);
        volume.profile.TryGet(out vignette);
    }

    protected override void OnIterationStart()
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = Random.Range(contrastMin, contrastMax);
            colorAdjustments.saturation.value = Random.Range(saturationMin, saturationMax);
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = Random.Range(grainIntensityMin, grainIntensityMax);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Random.Range(vignetteIntensityMin, vignetteIntensityMax);
        }
    }
}