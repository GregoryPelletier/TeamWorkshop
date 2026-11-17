// Anything that can be hit by the player or by an enemy (the player and every enemy).
// The fireball and the staff strike look for this, so they work on any creature.
public interface IHittable
{
    // Take one hit.
    void TakeHit();

    // True once this creature has been defeated.
    bool IsDefeated { get; }
}
