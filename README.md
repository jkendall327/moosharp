This is a little toy game engine for a MOO/MUD in C#.

Made because I got nostalgic for the weird old MUDs I used to play as a kid and thought it would be fun to try making one. It initially morphed into experimenting with implementing the actor pattern for concurrency (similar to Akka.NET et al), which was pretty fun to learn about (though it gave rise to some really confusing bugs).

I recently stripped out the actor pattern stuff because I wanted to try and make it actually playable, without dealing with all the boilerplate entailed.

This dovetailed with an idea of mine to make a gameworld with persistent LLM NPCs who walk around, interact with things, pursue goals and so on. I think a MOO is a great format for that kind of thing, since it lowers human players to the level of the AIs; that is, to the layer of raw text. We will see how it works out.

Right now the game loop is just one big foreach, so who knows how scaleable it is. I might refactor it back into doing the actor stuff down the line when I have actual mechanics nailed down. I will probably use Akka.NET then, instead of my own homegrown solution.
