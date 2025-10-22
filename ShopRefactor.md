# Planning document for shop system refactor
---
## Feature description
DEFINITELY: Furniture items separated into style-related groups. These can be viewed individually via a hud widget, or everything at once via fixed button on widget. Standard hud elements hidden, selector appears on far right of screen, to the right of the AT pane. 

MAYBE: Replace modded catalogues with packs, make base catalogue cheaper and *only* catalogue. (revamp appearance maybe?)

## Feature goals
- Easier browsing with large amounts of furniture mods
- Better load performance, avoid/ammortize shop bottleneck
- (?) More accessible decorating while maintaining progression

## Required changes
- catalogue must accept incremental additions
	- scrollbars must be responsive
	- searches must be responsive
	- selected index & AT pane must be responsive
	- time benching must be cumulative
- content must be grouped
	- combo generator must support grouping
	- groups must be favored over base catalogue
	- duplicate furniture must modify the group of existing item
- new widget
	- Add new widget
	- hide default right-side widgets
	- move and expand AT panel
- (?) catalogue replacement
	- add config option
	- remove known catalogue furnitures and replace with unlockers
	- unlocks shared for all players
	- architect's catalogue sourced from standard catalogue
	- remove membership card, keep mail in data but do not send.
	- resprite
	- new data asset
	- catalogue importer for spacecore/calcifer/FF