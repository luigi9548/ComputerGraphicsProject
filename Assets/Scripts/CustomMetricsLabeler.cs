using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;

// Labeler personalizzato che scrive le metriche custom nel dataset.
// Va aggiunto come Labeler sul componente Perception Camera (Add Labeler).
// Legge i valori esposti dai vari randomizer tramite i loro campi statici.
// I flag report* permettono di attivare solo le metriche dei randomizer
// effettivamente presenti nella scena.
public class CustomMetricsLabeler : CameraLabeler
{
    public override string description => "Reports custom per-frame metrics (lighting, camera, post-processing)";
    public override string labelerId => "custom_metrics";
    protected override bool supportsVisualization => false;

    [Header("Quali metriche registrare (attiva solo i randomizer presenti in scena)")]
    public bool reportLighting = true;
    public bool reportCamera = true;
    public bool reportPostProcessing = true;

    // Lighting
    private MetricDefinition lightingValuesDefinition;
    private MetricDefinition lightingPresetDefinition;
    // Camera
    private MetricDefinition cameraNameDefinition;
    private MetricDefinition cameraFovDefinition;
    // Post-processing
    private MetricDefinition postProcessingDefinition;

    protected override void Setup()
    {
        // ── Lighting ──
        if (reportLighting)
        {
            lightingValuesDefinition = new MetricDefinition(
                "LightingValues",
                "lighting_values",
                "Lighting numeric values: [intensity, temperature]");
            DatasetCapture.RegisterMetric(lightingValuesDefinition);

            lightingPresetDefinition = new MetricDefinition(
                "LightingPreset",
                "lighting_preset",
                "Name of the lighting preset used in the frame");
            DatasetCapture.RegisterMetric(lightingPresetDefinition);
        }

        // ── Camera ──
        if (reportCamera)
        {
            cameraNameDefinition = new MetricDefinition(
                "CameraName",
                "camera_name",
                "Name of the camera viewpoint used in the frame");
            DatasetCapture.RegisterMetric(cameraNameDefinition);

            cameraFovDefinition = new MetricDefinition(
                "CameraFov",
                "camera_fov",
                "Field of view of the camera in the frame");
            DatasetCapture.RegisterMetric(cameraFovDefinition);
        }

        // ── Post-processing ──
        if (reportPostProcessing)
        {
            postProcessingDefinition = new MetricDefinition(
                "PostProcessing",
                "post_processing",
                "Post-processing values: [contrast, saturation, grain, vignette]");
            DatasetCapture.RegisterMetric(postProcessingDefinition);
        }
    }

    protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
    {
        // ── Lighting ──
        if (reportLighting)
        {
            DatasetCapture.ReportMetric(lightingValuesDefinition,
                new GenericMetric(LightingRandomizer.CurrentLightingValues, lightingValuesDefinition));

            DatasetCapture.ReportMetric(lightingPresetDefinition,
                new GenericMetric(new[] { LightingRandomizer.CurrentPresetName }, lightingPresetDefinition));
        }

        // ── Camera ──
        if (reportCamera)
        {
            DatasetCapture.ReportMetric(cameraNameDefinition,
                new GenericMetric(new[] { CameraRandomizer.CurrentCameraName }, cameraNameDefinition));

            DatasetCapture.ReportMetric(cameraFovDefinition,
                new GenericMetric(new[] { CameraRandomizer.CurrentCameraFov }, cameraFovDefinition));
        }

        // ── Post-processing ──
        if (reportPostProcessing)
        {
            DatasetCapture.ReportMetric(postProcessingDefinition,
                new GenericMetric(PostProcessingRandomizer.CurrentPostProcessing, postProcessingDefinition));
        }
    }
}