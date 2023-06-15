# VRCMarker

## Features
- Custom Trail Renderer written in Udon, inspired by [Unity XR Line Renderer](https://github.com/Unity-Technologies/XRLineRenderer)
- Rounded Trail on PC and Quest
- Optimized with no overhead while idle
- Accurate Sync
- Smooth Gradients


## Installation

- Download the Unity Package from [releases](https://github.com/z3y/VRCMarker/releases)  and import into Unity
- Drag the Marker Prefab in your scene
- Adjust settings on the GameObject

Alternatively you can install with the package manager:
```
https://github.com/z3y/VRCMarker.git
```

> Requires UdonSharp v1 - [VCC](https://vcc.docs.vrchat.com/) Version

#

[Demo World](https://vrchat.com/home/world/wrld_df859907-113e-445b-9ec7-37c900c36c75)


![image](https://user-images.githubusercontent.com/33181641/194152223-e877ede1-6a6e-4a35-9223-a4a633e98c26.png)

![image](https://user-images.githubusercontent.com/33181641/194152197-a5647001-c29e-4231-a2f4-bf7858d2079a.png)


## How it works

The mesh for the trail renderer is created with the Unity Mesh API in Udon in the [MarkerTrail.cs](/Runtime/Scripts/MarkerTrail.cs) script. The data we set are vertices, normals and triangles. This data is later used in the [Trail Renderer.shader](/Runtime/Shader/Trail%20Renderer.shader) to create a trail.

A trail line consists of 2 parts: a line and a circle  (4 verts for the quad + 3 vertex for a triangle).

The vertices (Vector3[]) in the Mesh refer to the OS (object space) position of the vertex. The mesh transform is set to (0,0,0) at start which means that OS and WS (world space) position will be the same. This avoids the cost of calculating it in Udon and vertex shader can skip OS to WS transform. The mesh bounds are set to infinite while drawing, and only properly calculated at the end of drawing.

The normals (Vector3[]) are used for encoding the position of the other end of the line, explained later. For the circles they are unused (0,0,0)

The triangles (int[]) represent the index of the vertex position array, from which we can get a position for the triangle vertex. The size of this array is always 3x of vertex[], each 3 in anti-clockwise order represent one triangle. This order is important for expanding our verticies later in the vertex shader and still have visible faces with back-face culling.

### The Circle
The circle is just a single triangle that has all 3 vertices positioned the same by Udon. It is expanded in the vertex shader to the proper position to create an equilateral triangle. We know where to move each vertex because we always set them in the same order in Udon and we can get this order with `SV_VertexID`. If we calculate `vertexID % 7` we get an uint in range [0, 6]. This will tell us the position where the vertex needs to get moved and whether it is a connecting line or a cirlce part of the trail (first 4 are for line, last 3 for the circle). Since we know where the center of our triangle is (the vertex position we set in udon) we can rotate around it so it always faces the camera position. This is done in 2 steps, where each one rotates it along 1 axis at a time, in WS to always be accurate on all cameras
To create a circle inside the triangle we use a cutout shader, the outer parts are just discarded in the fragment shader.

### The Line
The line has 4 vertices, 2 of which are positioned at the start of a trail line and 2 at the end. Each end knows where the position of the other end is because we encode this position data in the normals attribute in Udon (its just another vector3[] we can use it however we want in the shader). The 2 vertices on each side are expanded in the opposite directions which creates a visible line. We know where to move each vertex and keep it facing the camera, the same way as for the circle, from the vertexID. We also know the expand direction from the cross product of the vertex pos and the vertex pos at the other end (from vertex position and our "normals"). This is also billboarded in a way that allows it to rotate around the axis of the line while facing the camera position.

Since all our positions are set along the line, it allows us to just expand it in the shader, we can have a property for the size that multiplies the expand to adjust the line thickness.

### Circles + Lines
The 2 parts combined create a convincing trail:


### Gradients
Gradients are implemented entierly in the shader. We can easily create them because we have vertexID. Udon only needs to set gradient colors at start and we interpolate between them to always create a smooth color transition.