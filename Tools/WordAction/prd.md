## Word Classification Pipeline

You are classifying English words for a TextRPG game. Each word gets:
- **Actions**: effect tags with intensity 1-10 (what the word does)
- **Target**: who/where it hits (separate from actions)
- **Cost**: 0-10 balance cost (powerful/advantageous effects cost more)

### Actions (20 effect types)

**Elements:** Water, Fire, Earth, Wind, Light, Dark
**Physical:** Push, Heavy, Damage, Shock
**Status:** Slow, Burn, Freeze, Curse, Buff, Poison
**Special:** Heal, Shield, Summon, Time

### Targets (pick one per word)

- **SingleEnemy** — hits one enemy (default, most common)
- **SingleAlly** — targets one ally (heals, buffs)
- **Self** — self-only (self-heals, self-buffs)
- **AreaEnemies** — hits all enemies
- **AreaAllies** — buffs/heals all allies
- **AreaAll** — hits everyone including allies (powerful but risky)
- **MeleeInFront** — melee range, single target in front
- **MeleeArea** — melee range, area around self

### Balance Rules
- Cost reflects strategic advantage: high damage + good targeting = high cost
- AreaAll effects REDUCE cost (downside of hitting allies)
- AreaEnemies effects INCREASE cost (pure upside)
- Self-targeting is cheaper than SingleAlly or AreaAllies
- MeleeInFront is cheaper than SingleEnemy (requires positioning)
- Words with no game relevance: target "SingleEnemy", cost 0, actions []

### Value Guide
- 1-3 = weak/subtle connection
- 4-6 = moderate/clear connection
- 7-10 = strong/overwhelming connection

### Example
tsunami: Water(5), Damage(5), Push(2) | target: AreaAll | cost: 6
bandage: Heal(2) | target: Self | cost: 0
inferno: Fire(5), Burn(4), Damage(4), Light(2) | target: AreaEnemies | cost: 5

### Each iteration:
1. Run: `python Tools/WordAction/batch_next.py --count 150`
   - If output is `COMPLETE` → output DONE and stop
   - Otherwise it prints a JSON array of words to classify
2. Classify each word with actions, target, and cost
3. Pipe your JSON classification into: `python Tools/WordAction/batch_insert.py`
   - Format: `[{"word":"tsunami","target":"AreaAll","cost":6,"actions":[{"action":"Water","value":5},{"action":"Damage","value":5},{"action":"Push","value":2}]}]`
4. Run: `python Tools/WordAction/stats.py` to log progress
5. Continue to next batch

Output DONE when batch_next.py returns COMPLETE.
