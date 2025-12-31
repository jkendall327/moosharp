# Tasks

## 1. Implement `wait` command
**Goal**: Allow players and agents to pass a turn without acting.
- **Why**: Useful for pacing and roleplay. Essential for agents to have a "do nothing" option that isn't a hack.
- **Details**:
  - Add `WaitCommand`, `WaitCommandDefinition`, and `WaitHandler`.
  - It should output "Time passes..." to the actor.
  - It should broadcast nothing or a subtle message ("X waits.") to the room.
  - Update `SystemPrompt.hbs` to suggest `wait` instead of `<skip>`.

## 2. Implement `remember` and `recall` commands
**Goal**: Give players a persistent notepad.
- **Why**: Agents have limited context windows; they need a way to offload "long term memory" into the game world. Humans also benefit from a notepad.
- **Details**:
  - Add a `List<string> Memories` field to the `Player` class.
  - Update `PlayerSnapshotFactory` and `PlayerDto` to persist this list.
  - Implement `RememberCommand`: `remember <text>` adds text to the list.
  - Implement `RecallCommand`: `recall` lists all memories. `recall <index>` shows a specific one? Or just dump all.
  - Implement `ForgetCommand`: `forget <index>` to remove a memory.

## 3. Improve `help` command
**Goal**: Allow looking up help for specific commands.
- **Why**: Currently `help` dumps all commands. Players need to see details (usage, description) for specific commands.
- **Details**:
  - Modify `HelpCommand` to accept an optional argument.
  - If argument is present, look up the command in `CommandReference`.
  - Display the description and usage for that specific command.
  - If not found, show "Command not found."

## 4. Implement `yell` command
**Goal**: Allow communication across rooms.
- **Why**: `Say` is local, `Whisper` is 1-to-1. `Yell` adds a "local area" broadcast which is fun for game events or getting attention.
- **Details**:
  - Add `YellCommand`.
  - Logic: Get current room. Get all exits. For each exit, get the destination room.
  - Broadcast message to current room and all connected rooms.
  - Output: "You yell, '...'" / "Someone yells from nearby, '...'" / "X yells, '...'".
