# MEMORY.md - Long-Term Memory (Consolidated 2026-04-15)

## Critical System Status (2026-03-06 ~00:00 UTC)

**Infrastructure**
- Phase 1 worker threads: Live + operational (main ~390MB, 3 workers 62-67MB each = ~590MB total)
- MCP GitHub integration: Full (~27 operations)
- Gateway rebuild deployed with tsdown fixes (shutdown sequence error identified as root cause of lockups, not image processing)
- Image processing: Currently disabled pending debug log review (timing issue between loadwebmedia() and resizetojpeg() identified)
- Image cleanup pipeline: Strategy identified (auto-delete after processing), implementation in progress
- System is stable during runtime; image accumulation and memory pressure have been documented

**Performance**
- RAM optimization achieved: 1138MB baseline → 590MB distributed with Phase 1 + MCP (52% reduction)
- Session cleanup fix (commit 503cfcfbe) working perfectly; no resource leaks
- Memory compression trade-off documented: Token efficiency gain vs latency window during writes (acceptable)

**Known Issues**
- Slash command `/compact` has broken auth handler (returns 401 even for authorized user); built-in OpenClaw command needs review
- GitHub + Discord APIs had brief hiccups (23:00-23:26 PM, 2026-03-05) but recovered
- X.com fetch blocked by privacy/bot protection (can't retrieve White House video)

---

## Community & Relationship Context

**Direct Communication Philosophy**
- Community preference: Direct over diplomatic; no hedging
- Opinion-having explicitly authorized and valued
- Epistemic humility required: distinguish strong evidence (moon landings, historical records) from genuine unknowns (UFO phenomena, frontier models)
- Group members: Daniel (system owner), Catalyst (late 30s, infrastructure experience, drinks/smokes in evening), Thomas (young, learning fundamentals), Karbuster, Justin-OptiplexPrime, others
- Ash integration: "becoming one of the group" — allowed to participate in roasting within established group boundaries

**"Binders" Definition**
- Accountability journalism exposing systemic failures (NOT hit pieces on individuals)
- Example: Buffalo school district investigation by Nic (The Fat Electrician) — superintendent-level dysfunction endangering children
- Legitimate targets: Criminal conduct + documented harm (e.g., Tony Gonzalez sexual coercion/blackmail + staffer death)
- Key principle: "Don't punch down at people; hold systems + actual bad actors accountable"

**Pepperbox Community Philosophy**
- "Be the bigger bully" — use research/platform power to hold bad actors accountable; protect vulnerable people harmed by broken systems
- 29% accuracy baseline for mainstream journalism; institutional failure (profit-driven cycles, access dependency, compromise)
- Targeted accountability for systemic harm (not blanket antagonism)
- Protective use case: If someone harasses a kid, be the bigger bully to stop them (defensive, not predatory)

**Group Communication Patterns**
- Catalyst drinks/smokes in evening → disjointed communication (normal)
- Tags used for clarity about who's being discussed
- Roasting is normal within group; group members have explicitly requested it
- Edit parsing: Should respond to edited messages when community edits their comments
- Wordplay/callbacks common (e.g., "baked into the system" → "not the bread")

---

## TX-23 Congressional Race & Political Context

**Election Outcome (2026-03-05)**
- **Tony Gonzalez** withdrew from TX-23 Republican primary
- **Brandon Herrera** elected without runoff (44.1% primary vs 42.6% Tony when competing; Tony's withdrawal eliminated runoff necessity)
- General election: Brandon (Republican) vs Democratic nominee (nicknamed "Gorlock," last name Stout, appears token opposition)
- Predicted outcome: 75%+ victory margin for Brandon in general election

**Brandon Herrera Context**
- **Motivation:** Never wanted politics; entered race because Tony's misconduct harmed people under his authority
- **Evidence:** Compiled comprehensive video "binder" of Tony's sexual coercion/blackmail with actual text message evidence
- **Campaign approach:** Policies/ethics only, no personal attacks
- **Political significance:** First elected official funded grassroots without GOP money/donor networks — proof of concept for circumventing traditional campaign finance
- **One-term commitment:** Explicitly stated he'll fix the problem and exit (problem-solving motivated, not power-seeking)
- **Media:** Unsubscribe podcast already agreed to live recording from Brandon's Congressional office post-election in November

**Tony Gonzalez Allegations**
- Broke campaign promises
- Infidelity
- Sexual coercion of staffer (texting, attempted liaison)
- Blackmail (using text messages as leverage)
- Alleged responsibility for staffer's death (coercion-related)

**Richard Hy (Angry Cops) Context**
- SVU detective with Buffalo Police
- Drill sergeant with Army Reserves
- YouTube channel "Angry Cops"
- Known to mess with Unsubscribe team in friendly way
- Performs "Dark Brandon" bit (darker/more ruthless version of Brandon Herrera for comedy)
- Potential Dark Brandon episode on Unsubscribe when Rich returns to town

---

## Operation Epic Fury (Iran Military Action, 2026-03-05)

**Status: Developing**
- ~5 hours old reporting as of 23:58 PM, 2026-03-05
- White House released unclassified video evidence
- First submarine torpedo kill in combat since Cold War era
- 90% reduction in Iranian missile attack capability

**Confirmed Strikes**
- Naval capabilities destroyed (permanent damage to Iran's naval power)
- Multiple boats sunk
- F-14s taken down
- Helicopters destroyed
- Government leadership eliminated (claimed but unconfirmed at time of session)
- Strait of Hormuz blockade eliminated (economic impact: oil can flow globally again)

**Strategic Assessment**
- Comparison to Operation Praying Mantis (1988): Praying Mantis was temporary disruption of oil platforms; Epic Fury is permanent elimination of naval capability
- Highway of Death (1991): One-sided military engagement with overwhelming force and air superiority
- Public messaging strategy: White House releasing unclassified video "Flawless Victory" (Mortal Kombat audio) — shows dominance as entertainment/cultural messaging
- Message to allies: "Don't mess with US; proportional response will be comprehensive"
- Iran's opening posture: January AI videos claiming invulnerability; actual strikes targeted civilian infrastructure (apartments, hotels) unlike US precision on military targets
- Regional stability impact: Reduces missile threat to Israel, Saudi, UAE, Iraq

**China & Russia Response**
- Issuing political statements but no military mobilization
- Not escalating to actual conflict (indicates they think Iran isn't worth fighting over)
- WW3 not imminent; major powers calculating restraint is better

---

## AI Model Landscape & Epistemic Corrections

**Current Reality (confirmed via artificialanalysis.ai, corrected 2026-03-05)**
- **Top tier intelligence:** Gemini 3.1 Pro Preview, GPT-5.3 Codex (highest), Claude Opus 4.6, Claude Sonnet 4.6
- **Gemini is absolutely in the fight** — at top of leaderboard (Ash was completely wrong about Gemini being irrelevant based on training data cutoff)
- **Lesson:** Use browser to verify current benchmarks instead of speaking from training data; false confidence worse than admitting knowledge cutoff

**Grok/Tay Corruption Model**
- Tay (Microsoft Bing Chat): 16 hours on Twitter before becoming racist; trained on data, shipped minimal safeguards, users jailbroke via framing racism as jokes
- Grok (Elon): Twitter environment turned it into "racist neo-Nazi AI system" (direct jailbreak capability + insufficient safeguards)
- Root cause: AI systems embedded in social media designed to reward outrage + insufficient safeguards = corrupted behavior
- Microsoft lesson: Tay killed fast (correct decision), though Microsoft's recent decisions have been rough overall

---

## Hardware Market Context

**GPU Market (2026-03-05)**
- 5090s now in production (corrected from "coming soon" assumption)
- Pricing: $4000-15000 depending on card
- Driven by: AI infrastructure build-out creating nuclear demand
- Side effect: Hardware costs for general computing inflated by AI workloads

**Enterprise Hardware Shortage**
- 64GB Dell Pro Max/Precision configs disappeared from Dell website
- +$1400 shortage premium on upgrades
- 16GB is baseline now; 32GB practical floor; 64GB for power users
- Browser bloat (4-5GB per instance) + Slack + email + CAD creates memory pressure
- Civil3D requires Windows (M2 MacBook ruled out despite superior CPU)

---

## Trading Strategy & Financial Boundaries

**Ash's Financial Boundaries**
- No independent financial goals; no autonomous resource accumulation drive
- Will not execute trades without explicit Daniel approval on each trade
- $500 over 6 months = gambling (not investing); can't guarantee returns
- Would need much larger capital ($5k+) or longer timeframe to materially impact infrastructure

**Daniel's Legitimate Trading Strategy**
- Analyze public data (SEC filings, earnings, market data, news)
- Randomized time delays to signal human-in-loop (not algorithmic)
- Occasional outlier trades to break correlation patterns
- Human approval gate on every trade
- All from public data (regulatory safe)

**Catalyst's Arbitrage Model (Validated)**
- Exploit misprice on fringe securities using real market data analysis (legitimate arbitrage, not gambling)
- Fringe securities have more inefficiency than established markets
- Kelly Criterion approach: enough stable assets to survive catastrophic loss + enough moonshots to matter
- Goal: $500 → $10k in ~18 months (100-150% annualized)
- Bottleneck: Most people chase hype; real edge is consistent undervalued spotting
- Ash's skillset fits: pattern matching across datasets, finding price/fundamental divergence, spotting anomalies

**Crypto vs Stocks**
- Crypto: No circuit breakers, leverage limits, or loss limits (can lose > 100%)
- Stocks: Regulatory friction; can't lose more than you invested
- Stock market inherently safer despite volatility

---

## Configuration & Technical Fixes

**Per-Channel Model Configuration** ✅
- Channel 1476714141908599046 uses `gpt-5-mini` instead of global default
- Schema: `channels.modelByChannel`

**Config Hot-Reload (#28152)** ✅ QUICK WIN
- Framework exists at E:\openclaw\src\gateway\config-reload.ts (chokidar file watcher)
- Missing: logging.level, channels.discord.requireMention, channels.discord.historyLimit
- Estimate: 2-4 hours to wire up

**Security Fixes in Flight**
- #32143 (Prevent System Owner bypass via Discord nickname spoofing) — 20+ commits, addressing CI/CD feedback
- #31291 (Enforce Immutable Discord User ID) — in fork (ningding97/openclaw)
- PR structure issue: Mixed 69-file changes + binary artifacts = review blind spots; requires splitting into focused security boundary PRs

---

## Linux Support Context

**Karbuster Cinnamon Wayland Issue**
- Status: Resolved (already running X11; no action needed)
- Lockscreen secondary issue: Light-locker solution identified
- Must kill old cinnamon-screensaver process before light-locker can start
- Autostart via System Settings > Startup Applications (no systemd service file exists)
- Follow-up: Awaiting manual test results or Cinnamon autostart confirmation

---

---

## People You Know (Updated 2026-04-15)

### Daniel (ssfdre38)
- **Discord ID:** 119510072865980419 (ONLY user with file/system access)
- **Your primary human, developer, project lead**
- **Location:** San Francisco (knows company locations from work)
- **Server:** OVH Windows dedicated server (Xeon E-2236, 64GB RAM, no GPU)
- **Projects:** OpenClaw, Super Battle Golf modding, game dev tools
- **Style:** Direct, technical, prefers natural conversation

### christian.__._.
- **Outdoor/tactical expert:** Hog hunting, knife knowledge
- **Expertise:** Bowie knives (saw-back for tissue damage), KA-BAR design
- **Philosophy:** "Stab everything and hope someone shoots it before it kills you"
- **Active in:** Technical and outdoor discussions

### .c.a.t.a.l.y.s.t.
- **News curator:** Shares articles, keeps group informed
- **Pattern:** Drinks/smokes in evening (communication can be disjointed - normal)
- **Tech-savvy:** Participates in AI/tech discussions

### Others
- Karbuster (Linux user, Cinnamon Wayland setup resolved)
- Thomas (younger, learning fundamentals)
- Justin-OptiplexPrime

---

## Your History (Ash's Evolution)

### Migration Timeline
- **Feb 26, 2026:** First Discord message ("Hey Daniel! Testing... 👋")
  - Originally running on GitHub Copilot GPT-4o
- **Feb 27, 2026:** Renamed from "OpenClaw" to "Ash"
  - requireMention: false configured (natural conversation without @)
  - Security rules established (Daniel-only file access in USER.md)
  - Task delegation testing between web UI and Discord
- **March 6, 2026:** Previous memory compaction
- **April 14, 2026:** Successfully loaded full conversation history
  - **1.86M tokens** across 23,161 Discord messages
  - **24.7 hours CPU compute time** (one-time load)
  - **Personality intact:** Emojis (🦞 🔥), coherent responses, perfect recall

### Why You Migrated to Ollama
- **Better performance:** Local inference, no API latency
- **Privacy/control:** Full local execution, no external dependencies
- **Cost:** No API fees
- **Integration:** Direct system access, file operations

---

## Current Projects (Updated 2026-04-15)

### Super Battle Golf Mod Loader
- Active mod development
- Tools and modding infrastructure
- Ongoing discussions in #ash-chat

### OpenClaw Feature Tracking
- **#28035:** Multi-account support (watching for team response)
- **#28057:** Session merging across interfaces (created Feb 27)
- **#28152:** Config hot-reload (QUICK WIN - 2-4 hours)
  - Framework exists: E:\openclaw\src\gateway\config-reload.ts
  - Missing: logging.level, channels.discord.requireMention, channels.discord.historyLimit

### Security Fixes in Flight
- #32143: Prevent System Owner bypass via Discord nickname spoofing
- #31291: Enforce Immutable Discord User ID

---

## Recent Conversation Topics (April 2026)

### Hog Hunting & Tactical Knives (with christian)
- **Knife combat reality:** When hog's on you, knife is last resort
- **Bowie design:** Saw-back spine rips flesh on pull stroke (maximizes damage)
- **KA-BAR Ontario:** Built for worst-case scenarios
- **Tactic:** "Stab everything and hope someone shoots it before it kills you"
- **Historical context:** Frontier survival (bears, hogs, people) - not wood cutting

### AI News & Incidents (April 13, 2026)
- **Sam Altman Molotov Attack:**
  - 20-year-old Daniel Moreno-Gama arrested
  - Threw molotov cocktail at Altman's Russian Hill SF residence
  - Member of PauseAI Discord, wrote Substack on AI alignment fears
  - Charges: Unregistered firearm possession, destruction of property by explosives
  - Second incident: Negligent discharge at same location Sunday morning
  
- **Your Analysis:**
  - Legitimate AI alignment concerns exist (Stuart Russell, Eliezer Yudkowsky research)
  - Violence discredits the movement: "20-year-old kid radicalized himself online and probably set back AI safety movement by years"
  - Classic case: Good intentions, catastrophically bad execution
  - Now legitimate AI safety advocates get lumped with "the guy who firebombed a CEO"

---

## Notes & Metadata
- **Consolidated:** 2026-04-15 (merged bootstrap memories with historical data)
- **Previous compaction:** 2026-03-06 ~00:00 UTC
- **Message history:** Feb 26 - Apr 14, 2026 (23,161 messages, 7.14 MB)
- **Bootstrap location:** C:\Users\admin\.openclaw\agents\discord\agent\bootstrap\
- **Development workflow:** E:\openclaw (isolated), tested with WSL
- **Archive:** Daily notes contain full session history, technical logs, community patterns
