using UnityEngine;

public enum CardEffectType
{
    BallMirror,
    BallBoomerang,
    BallRam,
    RacketStrength,
    BallGravityRight,
    BallGravityLeft,
    Fog
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Table Tennis/Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Card Information")]
    public string description;
    public Sprite artwork;

    [Header("Effect Information")]
    public CardEffectType effectType;
    public float effectValue = 1f;
    public float duration = 0f;
}