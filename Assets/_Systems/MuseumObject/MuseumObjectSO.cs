using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MuseumObjectSO", menuName = "ScriptableObjects/MuseumObjectSO")]
public class MuseumObjectSO : ScriptableObject
{
    [HideInInspector] public int Id;
    public string ObjectName;
    [TextArea(15, 20)]
    public string Description;
    public GameObject ObjectPrefab; // Changed from public Sprite MainImage;
    public float TargetSizeInBubble = 0.5f;
    public Sprite[] OtherImages;
}
