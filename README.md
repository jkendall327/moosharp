![MooSharp hero image](./image.png)

## [**Access the game here**](https://moosharp.fly.dev)

This is a little toy game engine for a MOO/MUD in C#.

Made because I got nostalgic for the weird old MUDs I used to play as a kid and thought it would be fun to try making one. It initially morphed into experimenting with implementing the actor pattern for concurrency (similar to Akka.NET et al), which was pretty fun to learn about (though it gave rise to some really confusing bugs).

I recently stripped out the actor pattern stuff because I wanted to try and make it actually playable, without dealing with all the boilerplate entailed.

This dovetailed with an idea of mine to make a gameworld with persistent LLM NPCs who walk around, interact with things, pursue goals and so on. I think a MOO is a great format for that kind of thing, since it lowers human players to the level of the AIs; that is, to the layer of raw text. We will see how it works out. I am always fascinated by these ominously new intelligences and want to see what they are capable of.

For that reason, the AIs in this game don't get any special tools (like privileged insight into the state of the world). They get the same raw text feed as humans do; they have to supply responses that meet the format of the game's parser.

Right now the game loop is just one big foreach, so who knows how scaleable it is. I might refactor it back into doing the actor stuff down the line when I have actual mechanics nailed down. I will probably use Akka.NET then, instead of my own homegrown solution.

You can look at `AGENTS.md` for some more detailed technical info.

When running the web app in Development, the agent OpenAI API key comes from user secrets. Set it from the MooSharp.Web project directory with:
- dotnet user-secrets set "Agents:OpenAIApiKey" "<your key>"
