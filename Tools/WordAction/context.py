#!/usr/bin/env python3
"""Generates classification reference context from DB + batch_insert.py.

Run before each classification batch to get up-to-date reference data.
Outputs everything the AI needs to classify words: valid values, action
descriptions, tag descriptions, archetypes, word families, and current
DB distribution summary.

Usage:
    py Tools/WordAction/context.py
"""

import os
import sqlite3
import sys
from collections import defaultdict

# Force UTF-8 on Windows
if sys.stdout.encoding != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8")
if sys.stderr.encoding != "utf-8":
    sys.stderr.reconfigure(encoding="utf-8")

SCRIPT_DIR = os.path.dirname(__file__)
DB_PATH = os.path.join(SCRIPT_DIR, "..", "..", "Assets", "StreamingAssets", "wordactions.db")

# Import validation sets from batch_insert.py (single source of truth)
from batch_insert import (
    VALID_ACTIONS, VALID_TAGS, VALID_TRIGGERS, VALID_EFFECTS,
    VALID_TARGETS, VALID_PASSIVE_TARGETS, VALID_ITEM_TYPES,
    VALID_STATUS_EFFECTS, VALID_UNIT_TYPES, VALID_AREAS,
)

# ── Action descriptions (what each action does, for classification) ──
ACTION_DESCRIPTIONS = {
    "Damage": "Physical damage (Str vs PDef). For physical attacks (punch, slash, crush). NOT for magical attacks — use MagicDamage.",
    "MagicDamage": "Magic damage (MagicPower vs MagicDefense). For ALL magical/elemental damage. Always pair with SPELL tag.",
    "Heal": "Restore HP (Value = amount).",
    "Burn": "Apply Burning DoT (Value = duration).",
    "Water": "Apply Wet status (Value = duration).",

    "Shock": "Lightning damage + bonus to Wet targets.",
    "Fear": "Apply Fear debuff (Value = duration).",
    "Stun": "Apply Stun (Value = duration).",
    "Freeze": "Apply Frostbitten — attacks last, cumulative MagicPower loss per tick (Value = stacks). Removes Burning.",
    "Concussion": "Apply Concussion stacking debuff (Value = stacks).",
    "Concentrate": "Restore mana + apply Concentrated buff (Value = mana amount).",
    "Bleed": "Apply Bleeding DoT — grows if untreated, heals reduce (Value = ignored, uses 999 duration).",
    "Summon": "Summon a unit (Value = creature level). Requires unit field with stats/passives. Structure-type: high HP, defensive passives. Offensive structures: attack abilities + offensive passives.",
    "Slow": "Slow target (Value = duration).",
    "BuffStrength": "Buff Strength stat (Value = amount).",
    "BuffMagicPower": "Buff Magic Power stat (Value = amount).",
    "BuffPhysicalDefense": "Buff Physical Defense (Value = amount).",
    "BuffMagicDefense": "Buff Magic Defense (Value = amount).",
    "BuffLuck": "Buff Luck stat (Value = amount).",
    "DebuffStrength": "Debuff Strength (Value = amount).",
    "DebuffMagicPower": "Debuff Magic Power (Value = amount).",
    "DebuffPhysicalDefense": "Debuff Physical Defense (Value = amount).",
    "DebuffMagicDefense": "Debuff Magic Defense (Value = amount).",
    "DebuffLuck": "Debuff Luck (Value = amount).",
    "Heavy": "Heavy impact (Value = intensity).",
    "Earth": "Earth elemental (Value = intensity).",
    "Dark": "Dark magic (Value = intensity).",
    "Light": "Light magic (Value = intensity).",
    "Curse": "Apply curse debuff (Value = duration).",
    "Poison": "Apply poison DoT (Value = duration).",
    "Grow": "Apply Growing regen — heals per tick, bonus when Wet (Value = duration).",
    "Thorns": "Apply Thorns — retaliates damage to attackers (Value = duration).",
    "Reflect": "Apply Reflecting — redirects single-target abilities to caster (Value = stacks).",
    "Hardening": "Apply Hardening — flat damage reduction, decays each turn (Value = stacks).",
    "Drunk": "Apply Drunk — scrambles keyboard letters. Value = stacks (5 letters/stack). Applied to SELF.",
    "Shield": "Apply shield (Value = amount).",
    "Item": "Equippable item → inventory. Value = durability (0 = infinite). Requires item field. Weapon-type uses assoc_word for ammo. Consumable auto-equips.",
    "Melt": "Debuffs physical defense (Value = amount). Works on meltable targets in events.",
    "Smash": "Heavy physical damage, breaks breakable objects (Value = damage).",
    "Pay": "Spend gold — bribery, trading, recruitment. Target Self. Value = gold. Used with 'give' prefix.",
    "Energize": "Apply Energetic — next word's actions fire 2x at half value, then Tired(1). Value = duration.",
    "Relax": "Noop — RELAX-tagged words remove 1 Anxiety stack. Value ignored.",
    "Sleep": "Apply Sleep to self — skip turns, wake on damage (20% resist/stack). Value = stacks.",
    "RestHeal": "Heal self. Base + 5 if no enemies + 10 if DWELLING present. MagicPower/3 scaling.",
    "Scramble": "Swap target's slot position with another unit. Disrupts formations. Value ignored (use 1).",
    "Time": "Time manipulation (Value = intensity).",
    "Peck": "Physical damage (Str vs PDef) + apply Bleeding. Value = base damage. For sharp animal attacks. BEAST/MELEE tag.",
    "Screech": "Apply Fear + Concussion(1). Value = Fear duration. For creature shrieks. BEAST tag.",
    "Purify": "Remove up to Value negative statuses. Self/AllAlliesAndSelf target. CLEANSING/HOLY tag.",
    "Awaken": "Remove Sleep/Stun/Frostbitten + apply Awakened(+1 all stats, 1 turn). Value ignored. LIGHT/HOLY tag.",
    "Siphon": "Steal random stat from target, give to caster. Value = amount per stat. DEBUFF/STEALTH/PSYCHIC tag.",
    "Deceive": "Apply Fear(value) + Concussion(permanent). For tricking/misleading. PSYCHIC/SHADOW/DEBUFF tag.",
    "Recuperate": "Heal (MagicPower-scaled) + remove 1 random negative status. Self/AllAlliesAndSelf. RESTORATION/CLEANSING/SUPPORT.",
    "Comfort": "Apply Energetic to target (not self). Grants extra turn. SUPPORT/SOCIAL tag.",
    "Overcharge": "Buff MagicPower(Value) + Energetic. Power-up before Shock/MagicDamage. ELEMENTAL/LIGHTNING/SPELL tag.",
    "Cannonade": "Multi-hit: fires Value shots at random enemies, 1 base damage each (Str-scaled). NAVAL/OFFENSIVE tag.",
    "Plunder": "Physical damage + steal 1 random stat. Pirate/raider words. OFFENSIVE/SHADOW/MELEE tag.",
    "Attune": "Save word's letters as attuned charges. Future words with attuned letters get +20% power. Value ignored. Self target. ARCANE/SPELL/SUPPORT.",
    "Ignite": "Magic damage + apply Burning(Value). Fire Peck variant. FIRE/ELEMENTAL tag.",
    "Combust": "Detonate Burning on target: if Burning, remove + bonus MagicDamage(value+stacks). Else base MagicDamage. FIRE/ELEMENTAL tag.",
    "Cataclysm": "Massive magic damage to ALL (enemies+allies). High risk AoE nuke. Target All. COSMIC/DESTRUCTION/FIRE/ARCANE tag.",
    "Cleave": "Physical damage to primary + half damage to random other enemy. Sweeping melee. BLADE/MELEE/PHYSICAL tag.",
    "Lockpick": "Pick locks — multi-step, Dex+Luck chance. On success triggers Open chain. High mana (~8). TOOL/STEALTH tag.",
    "Sunder": "Reduce target's armor/defense permanently. Value = defense reduction.",
    "Silence": "Apply Silenced — prevents spellcasting. Value = duration.",
    "Cauterize": "Remove all Bleeding from target. FIRE/MEDICAL tag. Value ignored.",
    "Charm": "Charm/enchant target — social manipulation.",
    # Interaction actions
    "Enter": "Enter/go to a place (event encounters). Value always 1.",
    "Talk": "Speak to an NPC (event encounters). Value always 1.",
    "Steal": "Attempt to steal from target (event encounters). Value always 1.",
    "Search": "Search/examine something (event encounters). Value always 1.",
    "Pray": "Pray at a shrine/altar (event encounters). Value always 1.",
    "Rest": "Rest or sleep (event encounters, target Self). Value always 1.",
    "Open": "Open a container/door (event encounters). Value always 1.",
    "Trade": "Trade with a merchant (event encounters). Value always 1.",
    "Recruit": "Recruit/hire an ally (event encounters). Value always 1.",
    "Leave": "Leave/exit current area (event encounters). Value always 1.",
}

# ── Tag descriptions ──
TAG_DESCRIPTIONS = {
    "MELEE": "Close-combat physical attacks. Triggers Warrior's 'Brute Force' passive (+50% damage).",
    "SOCIAL": "Social interaction words. Triggers Merchant's 'Charming Presence' passive (grants Shield).",
    "BEAST": "Animals, creatures, monsters (wolf, hawk, serpent, bear).",
    "FLYING": "Airborne creatures, wind-related (hawk, eagle, bat, owl).",
    "LIGHT": "Light, radiance, dawn. Distinct from HOLY (not religious).",
    "CLEANSING": "Purification, removal, washing away debuffs.",
    "DEBUFF": "Weakening, draining, sabotaging.",
    "STEALTH": "Sneaky, covert, hidden.",
    "LIGHTNING": "Electrical, shock, voltage.",
    "WEATHER": "Atmospheric phenomena, storms, climate.",
    "BOTANICAL": "Plants, flowers, herbs, vegetation.",
    "DRAIN": "Life-draining, parasitic, energy-sapping.",
    "FIRE": "Fire, flame, heat, combustion.",
    "COSMIC": "Stars, space, celestial phenomena.",
    "DESTRUCTION": "Devastating force, ruin, annihilation.",
    "BLADE": "Bladed weapons, slashing attacks.",
    "JUNGLE": "Tropical, rainforest, exotic wildlife.",
    "NAVAL": "Ships, sea combat, maritime.",
    "UNDEAD": "Ghosts, skeletons, necromancy.",
    "NATURE": "Natural world, organic, wilderness.",
    "ELEMENTAL": "Elemental forces (fire, water, earth, wind).",
    "OFFENSIVE": "Aggressive, attack-focused.",
    "RESTORATION": "Healing, recovery, mending.",
    "SHADOW": "Darkness, shadow magic, stealth.",
    "PHYSICAL": "Physical force, strength-based.",
    "DEFENSIVE": "Protection, guarding, shielding.",
    "ARCANE": "Mystical, scholarly, magical knowledge.",
    "HOLY": "Sacred, divine, religious.",
    "SUPPORT": "Buffing, assisting, enabling allies.",
    "PSYCHIC": "Mind, mental, illusion.",
    "THOUGHTS": "Thinking, contemplation, intellectual.",
    "RELAX": "Calm, rest, relaxation. Removes Anxiety stacks.",
    "DWELLING": "Homes, shelters, living spaces. Boosts RestHeal.",
    "SPELL": "Magical spells and incantations.",
    "SIGHT": "Vision, observation, telescopes.",
    "MEDICAL": "Medicine, surgery, wound treatment.",
    "NULLIFY": "Cancellation, negation, nullification.",
    "CONTROL": "Crowd control, restraint, manipulation.",
    "TOOL": "Tools, instruments, lockpicks.",
    "AIR": "Wind, breath, flight, atmosphere. Air-based attacks and movement.",
    "CRAFT": "Smithing, metalwork, creation, forging.",
}

# ── Word family pre-classifications ──
WORD_FAMILIES = {
    "Peck": "peck, pecks, nip, nips, nibble, nibbles, jab, jabs, bite, bites, claw, claws, talon, talons, fang, fangs, gouge, gouges",
    "Screech": "screech, shriek, shrikes, howl, howls, wail, wails, roar, roars, bellow, bellows, screams, cry, cries",
    "Purify": "purify, cleanse, cure, remedy, antidote, baptize, absolve, wash, rinse, disinfect, sanitize, sterilize, detox, detoxify, exorcise, dispel",
    "Awaken": "awaken, rouse, wake, stir, revive, arouse, invigorate, resuscitate, animate, enliven, vivify, rally, rejuvenate",
    "Siphon": "siphon, drain, leech, absorb, steal, pilfer, extract, devour, vampiric, parasitic, latch, suck, tap",
    "Vampiric(multi-target)": "absorb, drain, leech, devour, siphon, consume, lifestealer, bloodthirst",
    "Sacrifice(multi-target)": "sacrifice, immolate, martyr, kamikaze, offering, bloodpact",
    "Rally(multi-target)": "warcry, battlecry, rally, inspire, intimidate",
    "Deceive": "deceive, trick, mislead, confuse, bewilder, bamboozle, dupe, hoodwink, bluff, feint, distract, misdirect, beguile",
    "Summon(raven)": "raven, ravens, crow, crows, jackdaw, magpie, corvid",
    "Summon(treasonist)": "treasonist, treasonists, spy, spies, traitor, traitors, saboteur, saboteurs, infiltrator, mole",
    "Summon(lounge)": "lounge, lounges, sofa, couch, settee, hammock, recliner, divan",
    "Summon(twinflower)": "twinflower, twinflowers",
    "Recuperate": "recuperate, recover, unwind, convalesce, nurse, rehabilitate, convalescent, restful, revitalize, renew",
    "Comfort": "comfort, encourage, reassure, inspire, uplift, embolden, motivate, hearten, bolster, rally",
    "Overcharge": "overcharge, surge, jolt, electrify, galvanize, supercharge, amplify, boost",
    "Summon(ghostship)": "ghostship, galleon",
    "Cannonade": "cannonade, volley, barrage, bombardment, salvo, broadside, fusillade, battery, shelling",
    "Plunder": "plunder, pillage, loot, raid, maraud, ransack, rob, pirate, buccaneer, brigand, corsair",
    "Attune": "attune, harmonize, resonate, synchronize, calibrate, align, tune, chord, frequency, attunement, resound, reverberate",
    "Boil(burn+water+damage)": "boil, steam, geyser, simmer, seethe, blister, stew, brew, kettle, cauldron, scald, parboil, blanch",
    "Ignite": "kindle, inflame, sear, char, singe, incinerate",
    "Cauterize": "cauterize, cautery, tourniquet, compress, staunch, suture, styptic, clot, coagulate, brand, scald, salve, poultice",
    "Combust": "detonate, explode, erupt, rupture, burst, implode",
    "Summon(firemaster)": "firemaster, firemasters",
    "Summon(mercenary)": "mercenary, mercenaries, soldier, sellsword, bodyguard, gladiator, enforcer, warden, conscript, knight",
    "Cataclysm": "supernova, supernovas, apocalypse, armageddon, cataclysm, annihilation, obliteration, doomsday, ragnarok, catastrophe, holocaust, extinction, devastation, calamity",
    "Cleave": "machete, machetes, axe, hatchet, cleaver, scythe, saber, katana, broadsword, falchion, glaive, halberd, slash, hack, chop, carve, sever, bisect, rend",
    "Item(telescope)": "telescope, telescopes, spyglass, binoculars, lens, scope, monocle, periscope, prism, spectacles, microscope, beacon",
    "Lockpick": "lockpick, jimmy, picklock, skeleton_key, crowbar, jemmy, shiv, probe, tumbler, bypass, latch, keyhole, pin",
    "Bleed(visceral)": "viscerous, gore, maul, mutilate, savage, ravage, rend, disembowel, eviscerate, mangle, butcher",
    "Scramble+Damage(flutter)": "flutter, flurry, whirl, swirl, twirl, tumble, flit, dash, scatter, flicker",
    "Summon(forger)": "forger, forgers, smith, anvil, blacksmith, foundry, crucible, kiln",
}

TAG_FAMILIES = {
    "BEAST": "wolf, wolves, hawk, hawks, serpent, serpents, bear, bears, spider, spiders, eagle, eagles, falcon, falcons, vulture, hyena, panther, tiger, lion, shark",
    "FLYING": "hawk, hawks, eagle, eagles, falcon, falcons, owl, owls, bat, bats, moth, butterfly, dragonfly, fairy, sprite, phoenix, griffin, pegasus, wyvern",
    "SIGHT": "telescope, spyglass, binoculars, lens, scope, monocle, periscope, prism, spectacles, microscope, beacon, lantern, spotlight, observatory, lookout, watchtower",
    "LIGHT": "glow, shine, flash, beam, ray, sunrise, aurora, luminous, bright, radiant, brilliant, gleam, shimmer, sparkle, dazzle, illuminate, lantern, torch, candle, lighthouse",
    "CLEANSING": "purify, wash, rinse, cleanse, baptize, scrub, disinfect, cure, remedy, sanitize, sterilize, detox, soap, lather, bathe, shower, launder",
    "MEDICAL": "cautery, cauterize, tourniquet, compress, suture, styptic, scalpel, syringe, bandage, triage, vaccine, serum, salve, poultice, splint, gauze, ointment",
    "DEBUFF": "weaken, corrode, rust, wither, decay, sap, drain, curse, hex, blight, cripple, enfeeble, diminish, erode, siphon, leech, undermine, sabotage, impair",
    "STEALTH": "spy, assassin, thief, rogue, ninja, ghost, phantom, lurk, skulk, ambush, sneak, prowl, shadow, vanish, cloak, disguise, infiltrate, camouflage",
    "LIGHTNING": "lightning, thunder, bolt, spark, shock, jolt, zap, surge, voltage, static, electrify, galvanize, thunderbolt, thunderclap, electrode",
    "WEATHER": "storm, rain, hail, blizzard, tornado, cyclone, gale, fog, thunder, hurricane, monsoon, tempest, drought, frost, sleet, typhoon, squall",
    "RELAX": "lounge, relax, unwind, chill, rest, nap, doze, laze, idle, meditate, recline, slouch, snooze, siesta",
    "BOTANICAL": "rose, vine, bloom, sprout, petal, moss, fern, orchid, lily, tulip, blossom, seedling, root, ivy, herb, flora",
    "DRAIN": "leech, drain, absorb, siphon, sap, parasite, devour, consume, extract, deplete, wither, vampire",
    "UNDEAD": "ghost, phantom, specter, wraith, skeleton, zombie, lich, vampire, revenant, banshee, necromancer, corpse, ghoul, mummy, apparition",
    "NAVAL": "ship, anchor, cannon, sail, hull, mast, keel, stern, bow, fleet, armada, corsair, pirate, buccaneer, galleon, frigate, captain, sailor",
    "FIRE": "fire, flame, blaze, inferno, scorch, ember, torch, pyre, ash, cinder, combustion, magma, lava, volcano, kindle, ignite, combust, furnace, forge, smelt",
    "COSMIC": "supernova, comet, meteor, asteroid, nebula, galaxy, star, pulsar, quasar, cosmos, celestial, stellar, astral, eclipse, void, singularity, orbit",
    "DESTRUCTION": "supernova, apocalypse, armageddon, cataclysm, annihilation, obliteration, demolish, devastation, ruin, havoc, wreckage, carnage, rampage, ravage",
    "BLADE": "machete, axe, sword, dagger, knife, scythe, saber, katana, rapier, cutlass, cleaver, hatchet, scimitar, broadsword, falchion, glaive, halberd, stiletto, dirk",
    "JUNGLE": "machete, vine, python, piranha, jaguar, mosquito, canopy, swamp, tropical, bamboo, parrot, monkey, toucan, anaconda, orchid, fern, humidity, rainforest",
    "TOOL": "lockpick, jimmy, picklock, skeleton_key, crowbar, jemmy, shiv, probe, tumbler, bypass, latch, keyhole, pin, wrench, pliers, hammer, chisel, screwdriver",
    "AIR": "flutter, breeze, gust, gale, zephyr, cyclone, draft, updraft, exhale, inhale, soar, glide, whirl, swirl, gust, tornado, hurricane, tempest, squall, waft",
    "CRAFT": "forge, forger, smith, anvil, hammer, weld, solder, rivet, temper, anneal, crucible, kiln, foundry, bellows, tongs, ingot, alloy, smelt",
}


def pct(n, total):
    return f"{n / total * 100:.1f}%" if total > 0 else "0.0%"


def section(title):
    print(f"\n{'=' * 60}")
    print(f"  {title}")
    print(f"{'=' * 60}")


def main():
    # ── 1. Valid values (from batch_insert.py — single source of truth) ──
    section("VALID ACTIONS")
    interaction_actions = {"Enter", "Talk", "Steal", "Search", "Pray", "Rest", "Open", "Trade", "Recruit", "Leave"}
    combat_actions = sorted(VALID_ACTIONS - interaction_actions)
    for action in combat_actions:
        desc = ACTION_DESCRIPTIONS.get(action, "(no description)")
        print(f"  {action:<25} {desc}")
    print(f"\n  --- Interaction Actions (event encounters, value always 1) ---")
    for action in sorted(interaction_actions):
        desc = ACTION_DESCRIPTIONS.get(action, "(no description)")
        print(f"  {action:<25} {desc}")

    section("VALID TARGETS")
    groups = {
        "Basic": ["Self", "SingleEnemy", "AllEnemies", "All", "AllAllies", "AllAlliesAndSelf"],
        "Positional": ["FrontEnemy", "MiddleEnemy", "BackEnemy"],
        "Random": ["RandomEnemy", "RandomAlly", "RandomAny"],
        "Stat-based": [t for t in sorted(VALID_TARGETS) if "Lowest" in t or "Highest" in t],
        "Status": [t for t in sorted(VALID_TARGETS) if "Status" in t],
        "Subset": ["HalfEnemiesRandom", "TwoRandomEnemies", "ThreeRandomEnemies"],
    }
    shown = set()
    for group, targets in groups.items():
        present = [t for t in targets if t in VALID_TARGETS]
        if present:
            print(f"  {group}: {', '.join(present)}")
            shown.update(present)
    remaining = sorted(VALID_TARGETS - shown)
    if remaining:
        print(f"  Other: {', '.join(remaining)}")
    print(f"\n  Composite: BaseType+StatusEffect (e.g. AllEnemies+Burning, RandomEnemy+Wet)")
    print(f"  Aliases: Melee=FrontEnemy, Area=AllEnemies, AreaEnemies=AllEnemies, AreaAll=All")

    section("PER-ACTION TARGETING PATTERNS")
    print("  EVERY action can target independently. Burn(Self), Drunk(Enemy), Shield(AllAllies),")
    print("  Poison(Self), Stun(FrontEnemy) — any action + any target. Be creative!")
    print()
    print("  Common patterns:")
    print("    Vampiric:      Damage(Enemy) + Heal(Self)                    — absorb, drain, leech")
    print("    Sacrifice:     Damage(Enemy,high) + Damage/Poison(Self)      — sacrifice, immolate, martyr")
    print("    Berserker:     Damage(AllEnemies) + BuffStrength(Self)        — rampage, frenzy, berserk")
    print("    Rally:         BuffStrength(AllAlliesAndSelf) + Fear(AllEnemies) — warcry, rally, inspire")
    print("    Guardian:      Shield(AllAllies) + Hardening(Self)            — protect, fortify, bulwark")
    print()
    print("  Creative patterns (think beyond the basics!):")
    print("    Cursed Gift:   Heal(Self) + Drunk/Poison(Self)               — moonshine, hemlock")
    print("    Overexertion:  BuffStrength(Self,high) + Bleed(Self)          — overexert, strain")
    print("    Friendly Fire: Damage(AllEnemies) + Damage(AllAllies)         — earthquake, shockwave")
    print("    Contagion:     Poison(AllEnemies) + Burn(Self)                — plague, outbreak")
    print("    Tactical:      Stun(FrontEnemy) + Damage(BackEnemy)           — flank, pincer")
    print("    Intimidation:  Fear(AllEnemies) + BuffStrength(Self)          — intimidate, menace")
    print("    Sabotage:      DebuffStrength(Enemy) + Shield(Self)           — sabotage, undermine")
    print()
    print("  Rules:")
    print("    - All actions same target? Use word-level target only (no per-action target)")
    print("    - Actions target different things? Each action MUST have explicit 'target' field")
    print("    - Word-level 'target' = primary target for UI/display purposes")
    print("    - At least 20% of multi-action words should use per-action targeting")

    section("VALID TAGS")
    for tag in sorted(VALID_TAGS):
        desc = TAG_DESCRIPTIONS.get(tag, "")
        print(f"  {tag:<15} {desc}")

    section("VALID STATUS EFFECTS")
    print(f"  {', '.join(sorted(VALID_STATUS_EFFECTS))}")

    section("PASSIVE SYSTEM")
    print(f"  Triggers: {', '.join(sorted(VALID_TRIGGERS))}")
    print(f"  Effects: {', '.join(sorted(VALID_EFFECTS))}")
    print(f"  Passive targets: {', '.join(sorted(VALID_PASSIVE_TARGETS))}")
    print(f"  Unit types: {', '.join(sorted(VALID_UNIT_TYPES))}")
    print(f"  Item types: {', '.join(sorted(VALID_ITEM_TYPES))}")
    print(f"  Areas (always use Single): {', '.join(sorted(VALID_AREAS))}")

    # ── 2. Word families ──
    section("WORD FAMILIES (pre-classified action hints)")
    print("  If a word appears here, use the listed action as primary classification.")
    for action, words in sorted(WORD_FAMILIES.items()):
        print(f"  {action:<22} → {words}")

    section("TAG FAMILIES (pre-classified tag hints)")
    for tag, words in sorted(TAG_FAMILIES.items()):
        print(f"  {tag:<15} → {words}")

    # ── 3. Current DB state (compact summary) ──
    if not os.path.exists(DB_PATH):
        print(f"\n  (DB not found at {DB_PATH} — skip distribution)")
        return

    conn = sqlite3.connect(DB_PATH)

    total_with_actions = conn.execute("SELECT COUNT(DISTINCT word) FROM word_actions").fetchone()[0]
    total_meta = conn.execute("SELECT COUNT(*) FROM word_meta").fetchone()[0]

    section("CURRENT DB DISTRIBUTION")
    print(f"  Words with actions: {total_with_actions:,}")
    print(f"  Total meta entries: {total_meta:,}")

    per_action_count = conn.execute(
        "SELECT COUNT(DISTINCT word) FROM word_actions WHERE target IS NOT NULL"
    ).fetchone()[0]
    print(f"  Words with per-action targeting: {per_action_count:,}")

    # Action distribution (compact)
    action_counts = dict(conn.execute(
        "SELECT action_name, COUNT(DISTINCT word) FROM word_actions GROUP BY action_name ORDER BY COUNT(DISTINCT word) DESC"
    ).fetchall())

    print(f"\n  Action usage (words):")
    unused = []
    under = []
    over = []
    for action in sorted(VALID_ACTIONS, key=lambda a: action_counts.get(a, 0), reverse=True):
        count = action_counts.get(action, 0)
        p = count / max(1, total_with_actions) * 100
        flag = ""
        if count == 0:
            unused.append(action)
            continue
        if p < 1:
            under.append(action)
            flag = " !"
        elif p > 25:
            over.append(action)
            flag = " !!"
        print(f"    {action:<25} {count:>5} ({p:.1f}%){flag}")

    if unused:
        print(f"\n  UNUSED actions (0 words): {', '.join(sorted(unused))}")
    if under:
        print(f"  Under-represented (<1%): {', '.join(sorted(under))}")
    if over:
        print(f"  Over-represented (>25%): {', '.join(sorted(over))}")

    # Target distribution (compact)
    target_counts = conn.execute(
        "SELECT target, COUNT(*) FROM word_meta GROUP BY target ORDER BY COUNT(*) DESC LIMIT 10"
    ).fetchall()
    print(f"\n  Top targets:")
    for target, count in target_counts:
        print(f"    {target:<30} {count:>5} ({count / max(1, total_meta) * 100:.1f}%)")

    # Tag distribution (compact)
    tag_counts = conn.execute(
        "SELECT tag, COUNT(DISTINCT word) FROM word_tags GROUP BY tag ORDER BY COUNT(DISTINCT word) DESC"
    ).fetchall()
    total_tagged = conn.execute("SELECT COUNT(DISTINCT word) FROM word_tags").fetchone()[0]
    unused_tags = VALID_TAGS - set(t for t, _ in tag_counts)
    under_tags = [t for t, c in tag_counts if c / max(1, total_tagged) < 0.03]

    print(f"\n  Tag coverage ({total_tagged:,} tagged words):")
    for tag, count in tag_counts[:15]:
        print(f"    {tag:<15} {count:>5} ({count / max(1, total_tagged) * 100:.1f}%)")
    if len(tag_counts) > 15:
        print(f"    ... and {len(tag_counts) - 15} more tags")
    if unused_tags:
        print(f"  Unused tags: {', '.join(sorted(unused_tags))}")
    if under_tags:
        print(f"  Under-represented tags (<3%): {', '.join(sorted(under_tags))}")

    # Unit/Item counts
    try:
        unit_total = conn.execute("SELECT COUNT(*) FROM units").fetchone()[0]
        item_total = conn.execute("SELECT COUNT(*) FROM items").fetchone()[0]
        print(f"\n  Units: {unit_total}, Items: {item_total}")

        if item_total > 0:
            item_types = conn.execute(
                "SELECT item_type, COUNT(*) FROM items GROUP BY item_type ORDER BY COUNT(*) DESC"
            ).fetchall()
            unused_item_types = VALID_ITEM_TYPES - set(t for t, _ in item_types)
            print(f"  Item types: {', '.join(f'{t}:{c}' for t, c in item_types)}")
            if unused_item_types:
                print(f"  Unused item types: {', '.join(sorted(unused_item_types))}")
    except Exception:
        pass

    # Duplicate profiles (compact)
    dupe_count = conn.execute(
        "SELECT COUNT(*) FROM ("
        "  SELECT GROUP_CONCAT(action_name || ':' || value, ',') as profile "
        "  FROM (SELECT word, action_name, value FROM word_actions ORDER BY word, action_name, value) "
        "  GROUP BY word"
        ") t GROUP BY profile HAVING COUNT(*) > 1"
    ).fetchall()
    print(f"\n  Duplicate action profiles: {len(dupe_count)} groups")

    conn.close()


if __name__ == "__main__":
    main()
