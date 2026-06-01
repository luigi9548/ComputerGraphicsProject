using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Perception.Randomization.Randomizers;
using System;
using Random = UnityEngine.Random;

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

    // Valori applicati nell'iterazione corrente, letti dal CustomMetricsLabeler
    // Ordine: [contrast, saturation, grain, vignette]
    public static float[] CurrentPostProcessing = new float[] { 0f, 0f, 0f, 0f };

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
        // Calcola i valori una volta sola, cosi' li applichi e li registri coerenti
        float contrast = Random.Range(contrastMin, contrastMax);
        float saturation = Random.Range(saturationMin, saturationMax);
        float grain = Random.Range(grainIntensityMin, grainIntensityMax);
        float vignetteVal = Random.Range(vignetteIntensityMin, vignetteIntensityMax);

        if (colorAdjustments != null)
        {
            colorAdjustments.contrast.value = contrast;
            colorAdjustments.saturation.value = saturation;
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = grain;
        }

        if (vignette != null)
        {
            vignette.intensity.value = vignetteVal;
        }

        // Espone i valori applicati per il labeler
        CurrentPostProcessing = new float[] { contrast, saturation, grain, vignetteVal };
    }
}