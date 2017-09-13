# Duck Game Linux FNA patches
### Because Duck Game forgets to mkdir and wine is slow
##### MIT-licensed
----

[**Feel free to discuss this on Reddit!**](https://www.reddit.com/r/linux_gaming/comments/6zqrrx/duckgamelinux_fna_custom_steamworks_bindings_some/)

### Usage instructions:
* Get yourself a fresh copy of Duck Game, f.e. via Steam... through Wine... or from a friend with Windows.
* Install `mono-complete` and `libcurl3:i386` and `ffmpeg` (or matching) via your package manager.
    * Note: `ffmpeg`, not "`libav`" / `avconv`.
* Copy or symlink the i386 `libcurl.so.3` into `DuckGameDir/libcurl.so` because the Steam Runtime is somehow missing this...
* Download [**the latest released DuckGame-Linux-Complete.zip**](https://github.com/0x0ade/DuckGame-Linux/releases)
* Create a copy of the Duck Game directory... in the Duck Game directory and call it `orig`. XnaToFna will use that as a "backup" directory.
* Put the contents of `USEME` next to the rest of Duck Game. `XnaToFna.exe` and `DuckGame.exe` should be next to each other.
* Open terminal in Duck Game directory, run `chmod a+x ./mod.sh; ./mod.sh`
* Advanced users: Remove `Content` from `orig` (or don't copy in the first place) and _after_ the first patch, uncomment `--skip-xwb --skip-xgs` in `mod.sh`.
* Run `mono DuckGame.exe`
* Advanced users: [Set up MonoKickstart properly](https://github.com/flibitijibibo/MonoKickstart), launch the game via Steam.
* Be a duck with a gun!

### Current collection of patches:
* [XnaToFna](https://github.com/0x0ade/XnaToFna) gets the game running using [FNA](https://fna-xna.github.io/) instead of XNA. Thanks to [flibitijibibo](https://www.patreon.com/flibitijibibo) for FNA. Without him this wouldn't be possible!
* [Non-mixed-mode Steam.dll "proxy" to Steamworks.NET](https://github.com/0x0ade/DuckGame-Linux/tree/master/Steam) - this theoretically allows you to use Steam functionality natively... but the provided Steamworks.NET acts as "no DRM."
* More verbose error messages that help you when patching the game.
* Create missing directories automatically. Does Windows just implicitly create the directories?!
* Automatically pass -nothreading because it's faster.
* Automatically pass -nomods because the mods would need to be relinked to FNA. This doesn't happen automagically yet.

If for whatever reason something doesn't work, please create an issue on GitHub. I want this to work for everyone!
