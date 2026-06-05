using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.GroundTruth;
using Random = UnityEngine.Random;

[AddRandomizerMenu("Custom/Violence Randomizer")]
public class ViolenceRandomizer : Randomizer
{
    // Personaggio di un ruolo, con anchor di posizione opzionali.
    // Se entrambi gli anchor sono vuoti, la posizione NON viene toccata
    // (compatibile con personaggi mossi da altri randomizer, es. WalkPath).
    [Serializable]
    public class ViolenceActor
    {
        public GameObject character;

        [Header("Anchor di posizione (opzionali)")]
        [Tooltip("Anchor (GameObject vuoto) con posizione/rotazione per i due stati. " +
                 "Se entrambi vuoti la posizione non viene toccata. Se solo uno e' impostato, " +
                 "nell'altro stato il personaggio torna alla posizione originale.")]
        public Transform calmAnchor;
        public Transform violentAnchor;

        // Posa originale catturata a inizio scenario (fallback e ripristino)
        [NonSerialized] public Vector3 originalPosition;
        [NonSerialized] public Quaternion originalRotation;
    }

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
    public ViolenceActor[] victimVariants;
    public string victimViolentLabel = "victim";
    public string victimNonViolentLabel = "bystander";

    [Header("Passanti che reagiscono (solo animazione)")]
    public ViolenceActor[] reactingBystanders;

    [Header("Passanti che si girano verso la scena")]
    [Tooltip("Bystander che parlano tra loro e si voltano verso la violenza quando inizia")]
    public ViolenceActor[] turningBystanders;

    protected override void OnScenarioStart()
    {
        // Cattura la posa originale di tutti gli attori dei tre ruoli
        CaptureOriginals(victimVariants);
        CaptureOriginals(reactingBystanders);
        CaptureOriginals(turningBystanders);
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

        // 2. Vittime: posizione opzionale + animazione randomizzata + label dinamica
        foreach (var victim in victimVariants)
        {
            if (victim == null || victim.character == null) continue;

            ApplyAnchor(victim, isViolent, applyRotation: true);
            PlayRandomizedAnimation(victim.character, isViolent);
            SetLabel(victim.character, isViolent ? victimViolentLabel : victimNonViolentLabel);
        }

        // 3. Passanti che reagiscono: posizione opzionale + animazione
        foreach (var bystander in reactingBystanders)
        {
            if (bystander == null || bystander.character == null) continue;

            ApplyAnchor(bystander, isViolent, applyRotation: true);
            PlayRandomizedAnimation(bystander.character, isViolent);
        }

        // 4. Passanti che si girano: posizione opzionale + animazione + rotazione
        //    (la rotazione e' gestita da FaceTarget / posa originale, non dall'anchor)
        foreach (var b in turningBystanders)
        {
            if (b == null || b.character == null) continue;

            ApplyAnchor(b, isViolent, applyRotation: false);
            PlayRandomizedAnimation(b.character, isViolent);

            if (isViolent)
                FaceTarget(b.character);
            else
                b.character.transform.rotation = b.originalRotation; // posa di conversazione
        }
    }

    // Cattura posizione e rotazione iniziali di ogni attore
    private void CaptureOriginals(ViolenceActor[] actors)
    {
        if (actors == null) return;
        foreach (var a in actors)
        {
            if (a != null && a.character != null)
            {
                a.originalPosition = a.character.transform.position;
                a.originalRotation = a.character.transform.rotation;
            }
        }
    }

    // Applica la posizione in base agli anchor. Se nessun anchor e' impostato,
    // non tocca la posizione (cosi' non interferisce con altri randomizer).
    private void ApplyAnchor(ViolenceActor a, bool isViolent, bool applyRotation)
    {
        bool hasAnyAnchor = a.calmAnchor != null || a.violentAnchor != null;
        if (!hasAnyAnchor) return;

        Transform anchor = isViolent ? a.violentAnchor : a.calmAnchor;

        if (anchor != null)
        {
            a.character.transform.position = anchor.position;
            if (applyRotation)
                a.character.transform.rotation = anchor.rotation;
        }
        else
        {
            // Anchor mancante per questo stato: torna alla posa originale
            a.character.transform.position = a.originalPosition;
            if (applyRotation)
                a.character.transform.rotation = a.originalRotation;
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