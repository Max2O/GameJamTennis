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

public enum CardPersistence
{
    UntilNextSelection,
    Permanent,
    Timed
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Table Tennis/Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("AI Difficulty")]
    [Range(0f, 100f)]
    public float aiMissChance = 1f;
    [Header("Card Information")]
    public string description;
    public Sprite artwork;

    [Header("Effect Information")]
    public CardEffectType effectType;
    public float effectValue = 1f;
    public float duration = 0f;

    [Header("Persistence")]
    public CardPersistence persistence = CardPersistence.Permanent;
}