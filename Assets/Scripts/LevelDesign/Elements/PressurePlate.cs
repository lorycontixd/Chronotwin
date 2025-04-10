using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PressurePlateType
{
    Standard, // Activates while weight is applied, deactivates when weight is removed
    Toggle, // Changes state (on/off) each time it's pressed
    Timed,      // Remains activated for a set period after being pressed
    Sequence    // Must be activated in a specific order within a sequence group
}

public class PressurePlate : MonoBehaviour
{
    [Header("Plate Configuration")]
    [SerializeField] private PressurePlateType plateType = PressurePlateType.Standard;
    [SerializeField] private float activationWeight = 1f;  // Weight required to activate
    [SerializeField] private float activationTime = 3f;    // For timed plates
    [SerializeField] private int sequenceOrder = 0;        // For sequence plates
    [SerializeField] private string sequenceGroupID = "";  // Group ID for sequence plates
}
