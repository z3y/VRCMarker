This prefab requieres SDK3(2021.06.03.14.57 or newer) and UdonSharp.

How to use:
-extract the file and place it anywhere in your project
-drag in the example.prefab in your scene and modify it with your needs
-ink color or gradient can be changed on the pen gameobject
-pens can be duplicated as many times as you want without needing attidional materials or removed

Other prefabs like pens, eraser can be placed independantly and everything should still function the same.
The example prefab has a respawn button which resets the position of every child pen or eraser if they are not held
Lines are synced using manual sync. Their positions are getting sent over the network so on the both ends the lines should look exactly the same
The vertex color shader uses centroid sampling so no weird shimmering

Each line you write instantiates a new game object. There is no pooling system however other popular pen prefabs do the same and I havent ran into any performance issues.
If there's any issues dm z3y#3214
