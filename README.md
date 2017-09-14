# Duck Game Linux FNA patches
### Because Duck Game forgets to mkdir and wine is slow
##### MIT-licensed
----

[**Feel free to discuss this on Reddit!**](https://www.reddit.com/r/linux_gaming/comments/6zqrrx/duckgamelinux_fna_custom_steamworks_bindings_some/)  
[Screenshots](https://twitter.com/0x0ade/status/907745108010946560), [GamingOnLinux.com article](https://www.gamingonlinux.com/articles/want-to-play-duck-game-on-linux-well-its-possible-thanks-to-xnatofna.10339)

[**You can support me on Patreon!**](https://www.patreon.com/0x0ade)  
This project wouldn't be possible without the support from:
* Ethan Lee: Thank you for creating FNA!
* Artus Elias Meyer-Toms, Renaud Bedard and Ryan Kistner: I wouldn't be able to get my hands on the game without your support!

### Usage instructions:
**Preparations:**
* Get yourself a fresh copy of Duck Game, f.e. via Steam... through Wine... or from a friend with Windows.
* Create a copy of the Duck Game directory... in the Duck Game directory and call it `orig`. XnaToFna will use that as a "backup" directory.
* Install `mono-complete` and `libcurl3:i386` and `ffmpeg` (or matching) via your package manager.
    * Note: `ffmpeg`, not "`libav`" / `avconv`.
* Copy or symlink the i386 `libcurl.so.3` into `DuckGameDir/libcurl.so` because the Steam Runtime is somehow missing this...

**Installing / updating:**
* Download [**the latest released DuckGame-Linux-Complete.zip**](https://github.com/0x0ade/DuckGame-Linux/releases)
* Put the contents of the .zip next to the rest of Duck Game. `XnaToFna.exe` and `DuckGame.exe` should be next to each other.
* Open terminal in Duck Game directory, run `chmod a+x ./mod.sh; ./mod.sh`
* Run `mono DuckGame.exe` OR Launch the game via Steam (add `DuckGame.sh` to your library as "non-Steam game").
* Be a duck with a gun!

### Current collection of patches:
* [XnaToFna](https://github.com/0x0ade/XnaToFna) gets the game running using [FNA](https://fna-xna.github.io/) instead of XNA. Thanks to [flibitijibibo](https://www.patreon.com/flibitijibibo) for FNA. Without him this wouldn't be possible!
* [Non-mixed-mode Steam.dll "proxy" to Steamworks.NET](https://github.com/0x0ade/DuckGame-Linux/tree/master/Steam) - this allows you to use Steam functionality natively... although it still contains a few holes. Working on it!
* More verbose fatal error logging that help you when patching the game.
* Create missing directories automatically. Does Windows just implicitly create the directories?!
* Automatically pass -nothreading because it's faster.
* Automatically pass -nomods because the mods would need to be relinked to FNA. This doesn't happen automagically yet.
* Fix `ModLoader.modHash == null`, not `"nomods"` when `-nomods` is passed. This also affects vanilla Duck Game and can kill Steam.

If for whatever reason something doesn't work, please create an issue on GitHub. I want this to work for everyone!
