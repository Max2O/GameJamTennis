
using System.Collections.Generic;
using UnityEngine;

public class CardEffectManager : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private PlayerShot playerShot;

    // Each entry represents one acquired card effect.
    private class ActiveEffect
    {
        public CardData card;
        public float remainingDuration;
        public CardPersistence persistence;
    }

    public int RamBallStacks
    {
        get
        {
            int count = 0;

            foreach (ActiveEffect effect in activeEffects)
            {
                if (effect.card.effectType == CardEffectType.BallRam)
                    count++;
            }

            return count;
        }
    }

    public int BoomerangStacks
    {
        get
        {
            int count = 0;

            foreach (ActiveEffect effect in activeEffects)
            {
                if (effect.card.effectType == CardEffectType.BallBoomerang)
                    count++;
            }

            return count;
        }
    }

    private readonly List<ActiveEffect> activeEffects =
        new List<ActiveEffect>();

    // Called when the player or enemy acquires a card.
    public void ApplyCard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        ActiveEffect newEffect = new ActiveEffect
        {
            card = card,
            persistence = card.persistence,
            remainingDuration = card.duration
        };

        activeEffects.Add(newEffect);

        RefreshEffects();
    }

    private void Update()
    {
        bool effectsChanged = false;

        // Count down temporary effects.
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = activeEffects[i];

            if (effect.persistence != CardPersistence.Timed)
                continue;

            effect.remainingDuration -= Time.deltaTime;

            if (effect.remainingDuration <= 0f)
            {
                activeEffects.RemoveAt(i);
                effectsChanged = true;
            }
        }

        if (effectsChanged)
            RefreshEffects();
    }

    public void ClearSelectionEffects()
    {
        activeEffects.RemoveAll(
            effect => effect.persistence == CardPersistence.UntilNextSelection
        );

        RefreshEffects();
    }

    // Recalculates the currently active effects.
    private void RefreshEffects()
    {
        bool mirrorActive = false;
        float gravityBend = 0f;

        foreach (ActiveEffect effect in activeEffects)
        {
            switch (effect.card.effectType)
            {
                case CardEffectType.BallMirror:
                    mirrorActive = true;
                    break;

                case CardEffectType.BallGravityLeft:
                    gravityBend -= Mathf.Abs(effect.card.effectValue);
                    break;

                case CardEffectType.BallGravityRight:
                    gravityBend += Mathf.Abs(effect.card.effectValue);
                    break;
            }
        }

        if (playerShot != null)
        {
            playerShot.SetBallMirrorActive(mirrorActive);
            playerShot.SetGravityBend(gravityBend);
        }
    }

    // Removes every active effect. Useful when restarting a round.
    public void ClearAllEffects()
    {
        activeEffects.Clear();
        RefreshEffects();
    }
}