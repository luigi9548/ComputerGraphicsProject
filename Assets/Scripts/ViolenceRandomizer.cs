using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.GroundTruth;
using Random = UnityEngine.Random;

[AddRandomizerMenu("Custom/Violence Randomizer")]
public class ViolenceRandomizer : Randomizer
{
    [Range(0f, 1f)]
    public float violenceProbability = 0.5f;

    [Header("Nomi degli stati nell'Animator")]
    public string violentStateName = "Violent";
    public string calmStateName = "Calm";

    [Header("Punto verso cui guardano i bystander quando c'e violenza")]
    [Tooltip("Es. un GameObject vuoto al centro della scena di violenza")]
    public Transform violenceTarget;

    [Header("Aggressori (attivi solo se violento)")]
    public GameObject[] aggressorGroups;

    [Header("Vittime (label dinamica + animazione)")]
    public GameObject[] victimVariants;
    public string victimViolentLabel = "victim";
    public string victimNonViolentLabel = "standerby";

    [Header("Passanti che reagiscono (solo animazione)")]
    public GameObject[] reactingBystanders;

    [Header("Passanti che si girano verso la scena")]
    [Tooltip("Bystander che parlano tra loro e si voltano verso la violenza quando inizia")]
    public GameObject[] turningBystanders;

    // Memorizza la rotazione originale (di conversazione) di ogni turning bystander
    private Quaternion[] originalRotations;

    protected override void OnScenarioStart()
    {
        // Cattura la rotazione iniziale impostata nell'editor (i bystander che parlano)
        if (turningBystanders != null)
        {
            originalRotations = new Quaternion[turningBystanders.Length];
            for (int i = 0; i < turningBystanders.Length; i++)
            {
                if (turningBystanders[i] != null)
                    originalRotations[i] = turningBystanders[i].transform.rotation;
            }
        }
    }

    protected override void OnIterationStart()
    {
        bool isViolent = Random.value < violenceProbability;

        // 1. Aggressori: presenti solo se violento
        foreach (var group in aggressorGroups)
        {
            if (group != null)
                group.SetActive(isViolent);
        }

        // 2. Vittime: animazione randomizzata + label dinamica
        foreach (var victim in victimVariants)
        {
            if (victim == null) continue;
            PlayRandomizedAnimation(victim, isViolent);
            SetLabel(victim, isViolent ? victimViolentLabel : victimNonViolentLabel);
        }

        // 3. Passanti che reagiscono (solo animazione)
        foreach (var bystander in reactingBystanders)
        {
            if (bystander != null)
                PlayRandomizedAnimation(bystander, isViolent);
        }

        // 4. Passanti che si girano: animazione + rotazione
        if (turningBystanders != null)
        {
            for (int i = 0; i < turningBystanders.Length; i++)
            {
                GameObject b = turningBystanders[i];
                if (b == null) continue;

                PlayRandomizedAnimation(b, isViolent);

                if (isViolent)
                    FaceTarget(b);
                else if (originalRotations != null)
                    b.transform.rotation = originalRotations[i]; // ripristina la posa di conversazione
            }
        }
    }

    // Forza lo stato corretto e parte da un punto casuale del clip per pose varie
    private void PlayRandomizedAnimation(GameObject obj, bool isViolent)
    {
        Animator animator = obj.GetComponent<Animator>();
        if (animator == null) return;

        string stateName = isViolent ? violentStateName : calmStateName;
        animator.Play(stateName, 0, Random.value);
    }

    // Ruota il bystander verso il punto della violenza, solo sull'asse Y
    private void FaceTarget(GameObject obj)
    {
        if (violenceTarget == null) return;

        Vector3 dir = violenceTarget.position - obj.transform.position;
        dir.y = 0f; // mantiene il personaggio dritto, ruota solo orizzontalmente
        if (dir.sqrMagnitude > 0.001f)
            obj.transform.rotation = Quaternion.LookRotation(dir);
    }

    private void SetLabel(GameObject obj, string label)
    {
        Labeling labeling = obj.GetComponent<Labeling>();
        if (labeling == null) return;

        labeling.labels.Clear();
        labeling.labels.Add(label);
        labeling.RefreshLabeling();
    }
}