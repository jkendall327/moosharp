# Agent System Architecture

The agent system allows LLM-powered AI players to participate in the game world alongside humans. Agents receive the same text feed as human players and must respond using the same command syntax - they have no privileged access or special capabilities. The system is designed around asynchronous message processing with careful concurrency control.

**Core components and flow:**

At startup, `AgentSpawner` loads agent definitions from `agents.json` (name, persona, LLM source, cooldowns). For each agent, it uses `AgentFactory` to construct an `AgentBrain` instance and sends a `RegisterAgentCommand` to create the agent's `Player` entity in the game world.

Each `AgentBrain` manages two concurrent loops. The processing loop reads incoming game messages (via `AgentPlayerConnection`) from an internal channel and feeds them to `AgentCore`. The volition loop periodically checks if the agent has been idle too long and, if so, injects a volition prompt to encourage spontaneous action.

`AgentCore` maintains the conversation history and enforces action cooldowns. When a message arrives, it appends it to the `ChatHistory`, checks cooldown, yields an `AgentThinkingCommand` (to update UI), makes the LLM call via `AgentResponseProvider`, appends the response to history, and yields a `WorldCommand` containing the agent's command text.

`AgentPromptProvider` builds the system prompt using a Handlebars template (`SystemPrompt.hbs`) that includes the agent's name, persona, and dynamically-generated help text from `CommandReference`. This ensures agents always know the current command vocabulary.

`AgentResponseProvider` handles multiple LLM backends (OpenAI, Anthropic, Gemini, OpenRouter) by creating transient Semantic Kernel instances. It logs timing and returns `ChatMessageContent` that gets parsed into game commands. The provider uses different connectors per source but maintains a unified interface.

`AgentPlayerConnection` implements `IPlayerConnection` just like SignalR connections for humans. When game events are formatted and sent to this connection, they flow into the agent's internal message channel rather than to a websocket. This abstraction allows the game engine to treat agents and humans identically.

Concurrency is managed with a semaphore in `AgentCore.ProcessMessageAsync()` that locks during LLM calls, preventing message reordering. The history is trimmed to the most recent N messages (configured in `AgentOptions`) to stay within context limits, but the system prompt is always preserved.

Action cooldowns prevent agents from spamming commands. Volition cooldowns determine how often the volition loop checks for idle agents. If an agent hasn't acted in over 5 minutes and passes the volition check, it receives the configured volition prompt (typically "What do you want to do next?") to encourage autonomous behavior.

The result is agents that behave as autonomous players: they receive game output as text, reason about it using their LLM, emit text commands, and participate in the world without special treatment.
