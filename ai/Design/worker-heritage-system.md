# Worker Heritage System

Last checked against code: 2026-05-13.

## Concept

Heritage is a cultural lens, not a stat block.

The runtime stores heritage as `WorkerHeritageKind` and currently synchronizes it from the existing visual/portrait `WorkerRaceKind`, so portrait folders and cultural identity stay consistent.

Heritage may affect:

- thought intensity and formation speed for already-triggered thoughts;
- knowledge confidence, score, and formation speed for relevant memories;
- social signal strength and confidence for Noosphere-visible signals;
- UI flavor and how the player reads the city's population.

Heritage must never affect:

- work speed;
- salary;
- production output;
- staffing eligibility;
- hard restrictions;
- biological superiority or conflict mechanics.

## Peoples

### Rovians

Lens: roads, work, schedules, money, order, exchange.

Soft effects:

- stronger work, money, and transport thoughts;
- slightly faster and firmer knowledge around Labor Exchange, Warehouse, Docks, Gas Station, stops, work, money, and transport;
- stronger Work/Money/Transport social signals.

Example: a no-job thought does not appear because the resident is Rovian, but once the no-job condition exists, it feels sharper.

### Zelens

Lens: home, yard, family, children, nature, cleanliness, calm settlement.

Soft effects:

- stronger family, housing, litter, City Park, and care-related thoughts;
- slightly faster and firmer knowledge around Personal House, City Park, Kindergarten, Primary School, Secondary School, litter, cleanliness, and family topics;
- stronger Family/Housing/Litter/Need social signals.

Example: street litter does not create a thought by heritage alone, but an existing litter thought lands harder for a Zelen resident.

### Iskrians

Lens: conversation, knowledge, rumors, public memory, Noosphere meaning.

Soft effects:

- stronger social, knowledge-reflection, arrival, topic, and meaning thoughts;
- slightly faster and firmer conversation-topic and City Hall/public-meaning knowledge;
- stronger Social/Knowledge/Topic/City social signals, making Iskrians more visible in Noosphere summaries.

Example: a conversation topic still needs a social event, but an Iskrian tends to fix that topic into memory with more confidence.

## Caps

- Thought intensity heritage bonus: max `+10`.
- Thought/knowledge formation multiplier: min `0.9`.
- Knowledge score bonus: max `+8`.
- Knowledge confidence bonus: max `+10`.
- Social signal strength bonus: max `+6`.
- Social signal confidence bonus: max `+6`.
- Opinion delta modifier from thought heritage bias: max one extra step in the existing direction.

## Code Map

- Runtime identity and helpers: `Assets/Scripts/Runtime/Core/GameBootstrap.WorkerHeritage.cs`.
- Storage: `Assets/Scripts/Runtime/Core/GameBootstrap.RuntimeModels.cs`.
- Thought hook: `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerThoughtFormation.cs` and `GameBootstrap.WorkerThoughts.cs`.
- Knowledge hook: `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerKnowledgeFormation.cs`.
- Social signal hook: `Assets/Scripts/Runtime/Actors/GameBootstrap.SocialSignals.cs`.
- UI: `GameBootstrap.WorkerPerks.cs`, `GameBootstrap.QuickHud.WorkerFocus.cs`, `GameBootstrap.FleetCanvas.StatesScreen.cs`, `GameBootstrap.NoosphereVision.Clarity.cs`.
- Snapshots: `GameBootstrap.NoosphereSnapshots*.cs`.
