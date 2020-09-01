# Rotstein

Rotstein is a Minecraft's redstone-inspired simulation game where player builds logic circuits and.

# Controls

WASD.  
1, 2, 3, …, 0, Shift-1, Shift-2, …, Shift-0 -- pick tile in hotbar.  
LMB -- place tile.  
RMB -- remove tile.  
MMB -- activate tile (works on levers).  
R -- rotate selected tile.  
Z, X -- zoom in and out.  

Basically ciruits work like Minecraft's redstone.  
Tiles from left to right:  
Wood, Stone, Iron, Redstone Block, Redstone Wire, Redstone Brdige, NOT Gate, OR Gate, AND Gate, XOR Gate, Repeater, Lever.  
![hotbar](https://i.ibb.co/mGrM0sX/2020-09-01-16-00.png)

A full adder in Rotstein:  
![circuit example](https://i.ibb.co/k9TYSLX/Untitled.png)

~ or ` (Tilde or Grave) -- open console.

Rotstein commands are:
- `world save "name"` -- saves world to "name".rts
- `world load "name"` -- loads world from "name".rts
- `label new "text"` -- creates a label at Player's position with text "text".
- `label delete` -- deletes a label closest to player.
