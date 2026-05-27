using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Parameters;
using System;
using UnityEngine.Perception.GroundTruth;
using Random = UnityEngine.Random;

[AddRandomizerMenu("Custom/Character Randomizer")]
public class CharacterRandomizer : Randomizer
{
    [System.Serializable]
    public class CharacterSlot
    {
        public string slotName;
        public GameObject[] characterVariants;
    }

    public CharacterSlot[] slots;

    protected override void OnIterationStart()
    {
        foreach (var slot in slots)
        {
            if (slot.characterVariants == null || slot.characterVariants.Length == 0)
                continue;

            int index = Random.Range(0, slot.characterVariants.Length);

            for (int i = 0; i < slot.characterVariants.Length; i++)
            {
                bool isActive = (i == index);
                // Abilita/disabilita solo Renderer e Labeling
                var renderers = slot.characterVariants[i].GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    r.enabled = isActive;

                var labeling = slot.characterVariants[i].GetComponent<Labeling>();
                if (labeling != null)
                    labeling.enabled = isActive;
            }
        }
    }
}