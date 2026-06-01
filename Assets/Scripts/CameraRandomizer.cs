using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using Random = UnityEngine.Random;

[AddRandomizerMenu("Custom/Camera Randomizer")]
public class CameraRandomizer : Randomizer
{
    [System.Serializable]
    public class CameraPosition
    {
        public string positionName;
        public Transform basePosition;
        public float maxPositionOffset = 0.3f;
        public float maxRotationOffset = 5f;
        public float fovMin = 50f;
        public float fovMax = 80f;
    }

    public CameraPosition[] cameraPositions;

    private Camera mainCamera;

    // Valori correnti letti dal CustomMetricsLabeler
    public static string CurrentCameraName = "";
    public static float CurrentCameraFov = 0f;

    protected override void OnAwake()
    {
        mainCamera = Camera.main;
    }

    protected override void OnIterationStart()
    {
        if (cameraPositions == null || cameraPositions.Length == 0) return;

        // Scegli casualmente uno dei punti base
        int index = Random.Range(0, cameraPositions.Length);
        CameraPosition selected = cameraPositions[index];

        if (selected.basePosition == null) return;

        // Applica piccolo offset di posizione intorno al punto base
        Vector3 positionOffset = new Vector3(
            Random.Range(-selected.maxPositionOffset, selected.maxPositionOffset),
            Random.Range(-selected.maxPositionOffset * 0.5f, selected.maxPositionOffset * 0.5f),
            Random.Range(-selected.maxPositionOffset, selected.maxPositionOffset)
        );
        mainCamera.transform.position = selected.basePosition.position + positionOffset;

        // Applica piccola variazione di rotazione intorno alla rotazione base
        Vector3 baseRotation = selected.basePosition.rotation.eulerAngles;
        Vector3 rotationOffset = new Vector3(
            Random.Range(-selected.maxRotationOffset, selected.maxRotationOffset),
            Random.Range(-selected.maxRotationOffset, selected.maxRotationOffset),
            0
        );
        mainCamera.transform.rotation = Quaternion.Euler(baseRotation + rotationOffset);

        // Applica FOV casuale nel range definito per questo punto
        mainCamera.fieldOfView = Random.Range(selected.fovMin, selected.fovMax);

        // Espone i valori correnti per il labeler
        CurrentCameraName = selected.positionName;
        CurrentCameraFov = mainCamera.fieldOfView;
    }
}