# Duck Game Linux FNA patches
### Because Duck Game forgets to mkdir and wine is slow
##### MIT-licensed
----

### Usage instructions:
* Get yourself a fresh copy of Duck Game, f.e. via Steam... through Wine... or from a friend with Windows.
* Install `mono-complete` via your package manager.
* Download [the latest release DuckGame-Linux-Complete.zip](https://github.com/0x0ade/DuckGame-Linux/releases)
* Create a copy of the Duck Game directory... in the Duck Game directory and call it `orig`. XnaToFna will use that as a "backup" directory.
* Put the contents of `USEME` next to the rest of Duck Game. `XnaToFna.exe` and `DuckGame.exe` should be next to each other.
* Open terminal in Duck Game directory, run `chmod a+x ./mod.sh; ./mod.sh`
* Advanced users: Remove `Content` from `orig` (or don't copy in the first place) and _after_ the first patch, uncomment `--skip-xwb --skip-xgs` in `mod.sh`.
* Run `mono DuckGame.exe`
* Advanced users: [Set up MonoKickstart properly](https://github.com/flibitijibibo/MonoKickstart), replace the stubbed Steamworks.NET with a proper copy, launch the game via Steam.
* Be a duck with a gun!

### Current collection of patches:
* [XnaToFna](https://github.com/0x0ade/XnaToFna) gets the game running using FNA instead of XNA. Simple enough!
* [Non-mixed-mode Steam.dll "proxy" to Steamworks.NET](https://github.com/0x0ade/DuckGame-Linux/tree/master/Steam) - this theoretically allows you to use Steam functionality natively... but the provided Steamworks.NET acts as "no DRM."
* More verbose error messages that help you when patching the game.
* Create missing directories automatically. Does Windows just implicitly create the directories?!
* Automatically pass -nothreading because it's faster.
* Automatically pass -nomods because the mods would need to be relinked to FNA. This doesn't happen automagically yet.

If for whatever reason something doesn't work, please create an issue on GitHub. I want this to work for everyone!
