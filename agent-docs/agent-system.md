# Agent System Architecture

The agent system allows LLM-powered AI players to participate in the game world alongside humans. Agents receive the same text feed as human players and must respond using the same command syntax - they have no privileged access or special capabilities. The system is designed around asynchronous message processing with careful concurrency control.

**Core components and flow:**

At startup, `AgentSpawner` loads agent definitions from `agents.json` (name, persona, LLM source, cooldowns). For each agent, it uses `AgentFactory` to construct an `AgentBrain` instance. It ensures a `Player` entity exists for the agent in the database (creating one via `IPlayerRepository` if needed) and connects the agent to the game via `ISessionGateway.OnSessionStartedAsync`.

Each `AgentBrain` runs a main loop that waits on two signals: incoming game messages (via a `Channel<string>`) or a "boredom" timer. If a message arrives, it is processed immediately. If the timer expires (meaning the agent hasn't acted for a while), the brain triggers a "volition" check to see if the agent should act spontaneously.

`AgentCore` manages the agent's state, conversation history, and decision-making logic. When a message is processed, `AgentCore` appends it to the `ChatHistory` and calls the `AgentResponseProvider`. If the LLM generates a valid command, `AgentCore` yields an `InputCommand` which the `AgentBrain` dispatches to the game engine.

`AgentPromptProvider` builds the system prompt using a Handlebars template (`SystemPrompt.hbs`) that includes the agent's name, persona, and dynamically-generated help text from `CommandReference`. This ensures agents always know the current command vocabulary.

`AgentResponseProvider` handles multiple LLM backends (OpenAI, Anthropic, Gemini, OpenRouter) by creating transient Semantic Kernel instances or using SDK clients directly. It logs timing and returns `ChatMessageContent` that gets parsed into game commands.

`AgentOutputChannel` implements `IOutputChannel`. This is the bridge between the game engine and the agent. When the game sends a message to the agent (e.g. "You see a goblin."), `ISessionGateway` writes to this channel, which pushes the text into the `AgentBrain`'s inbox.

The system handles concurrency by processing messages sequentially within the `AgentBrain` loop. The history is trimmed to the most recent N messages (configured in `AgentOptions`) to stay within context limits, but the system prompt is always preserved.

Action cooldowns prevent agents from spamming commands. Volition cooldowns determine how often the loop checks for idle agents. If an agent hasn't acted in over 5 minutes (default) and passes the volition check, it receives the configured volition prompt (typically "What do you want to do next?") to encourage autonomous behavior.

The result is agents that behave as autonomous players: they receive game output as text, reason about it using their LLM, emit text commands, and participate in the world without special treatment.
