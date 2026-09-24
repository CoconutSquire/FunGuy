# Unrepresented Character Mechanics

This file records mechanics described by the character-design data that are **not currently first-class mechanics in the battle skill/effect system**.

The current skill integration wires all 71 characters to their documented signature skills, but these mechanics need dedicated runtime support rather than being approximated with generic damage/status effects.

## 1. Event-driven passive triggers

Several passives need to react automatically to battle events rather than when a skill is explicitly used.

Examples:
- **Golden Chanterelle** — gain a 10% Max HP shield when dealing damage.
- **Lion's Mane Monk** — chance to counterattack when hit.
- **Birch Polypore** — stronger healing when the target is a Tank.
- **Willow-Bracket** — create a bomb whenever an enemy moves.
- **Dead Man's Fingers** — gain lifesteal while below 80% HP.
- **Devil's Urn** — heal allies when enemies are revealed.

## 2. Persistent and stacking state

The data contains effects that persist indefinitely or accumulate from repeated events.

Examples:
- **Fly Agaric Jester** — basic attacks add permanent Ibotenic Acid stacks; the signature attack scales from those stacks.
- **Black-Yeasted Diver** — Poison stacks affect later damage.
- **Xylaria Bone-Breaker** — its debuff stacks on each hit and its DEF reduction is permanent.
- **Crimini Cadet** — permanently gains SPD after a multi-hit condition occurs.

The existing status model supports timed statuses, but these mechanics require explicit persistent-stack state and stack-aware scaling.

## 3. Conditional skill behavior

Some skills change their result based on the target or current battle state.

Examples:
- **Indigo Archer** — bonus damage against bleeding targets.
- **Blue Lichen Sniper** — attacks a frozen enemy again, up to 3 times.
- **Inky Cap Assassin** — instantly executes targets below 20% HP.
- **Dead Man's Fingers** — damage/healing behavior changes below 80% HP.
- **Crimini Cadet** — behavior depends on whether multiple hits occurred.

These require condition evaluation during skill resolution.

## 4. Execute / threshold effects

**Inky Cap Assassin** specifically calls for an instant execute below 20% HP.

This is different from ordinary damage: the effect must check the target's post/current HP against a threshold and resolve a special kill condition.

## 5. Traps, bombs, and board-occupancy triggers

The design includes effects that are created now and triggered later by movement or tile occupancy.

Examples:
- **Marsh Marasmius** — sets a trap that triggers when the next enemy moves.
- **Willow-Bracket** — leaves a mushroom bomb in the vacated space when an enemy moves; it detonates when that space becomes occupied.

These require persistent board objects plus movement/occupancy events.

## 6. Counterattacks and reactive attacks

**Lion's Mane Monk** can automatically perform a basic-attack counter when hit by an ability or basic attack.

This needs an event hook around incoming attacks, a chance roll, recursion protection, and a way to distinguish the triggering attack from the reaction.

## 7. Turn manipulation

**Star-Tip Scout** grants an ally an immediate extra turn.

This is distinct from gauge manipulation: it requires explicit insertion of an additional turn into the battle order without incorrectly consuming or resetting the normal turn.

## 8. Multi-hit / repeat resolution

**Slime-Mold Geologist** has a 50% chance for a skill to trigger again at 50% effectiveness.  
**Blue Lichen Sniper** can repeat its basic attack against frozen targets, up to 3 times.

These require bounded recursive/repeat skill resolution and scaling of subsequent casts.

## 9. Advanced shield interactions

Some characters do more than simply create shields:
- **Bleeding Tooth** — deals 2x damage to shields.
- **Golden Chanterelle** — creates a shield as a consequence of dealing damage.
- **Peat-Moss Shaman** — converts energy granted to allies into an equal Max-HP shield on itself.
- **Permafrost Sentinel** — gains Burn immunity while shielded.
- **Snow-Bank Hermit** — automatically intercepts the first ally hit each turn.

These require shield-aware damage, conditional shield state, conversion effects, and/or damage interception.

## 10. Lifesteal, HP theft, and conditional healing

The roster contains several forms of health transfer that are more specific than ordinary healing:
- **Dead Man's Fingers** — conditional lifesteal below 80% HP.
- **Swamp Truffle** — lifesteal against poisoned enemies.
- **Cup Fungus** — steals 5% HP from its target.
- **Lobster Fungus** — its healing behavior depends on its applied Regen/shield sequence.

These should eventually use explicit health-transfer/lifesteal effects rather than treating them as generic heals.

## 11. Reveal / stealth interaction

**Devil's Urn** reveals enemies in stealth and reacts to revealed enemies by healing allies. This requires a true reveal event/state rather than only applying or removing a Stealth status.

## 12. Randomized buff selection

**Shiitake Sensei** grants the team a random buff.

This requires a defined buff pool and deterministic battle RNG so the selected buff can be replayed/tested reliably.

---

## Implementation note

The current skill data can represent the simpler portions of these kits (damage, healing, shields, timed statuses, cleansing, dispelling, energy, gauge, and movement-like effects), but the mechanics above need dedicated engine-level support before their documented behavior can be considered exact.

Unity's JSON serializer is intentionally structured around fields that map to known serializable types, so adding these as explicit effect/state types is preferable to hiding arbitrary mechanics in unstructured JSON. citeturn0search0
