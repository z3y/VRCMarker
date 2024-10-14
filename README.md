# VRCMarker

Get the new improved pro version here [z3y.gumroad.com/l/vrcm](https://z3y.gumroad.com/l/vrcm)

## Features

### Free

- Custom Line Renderer with rounded lines on both PC and Quest
- Accurate Sync
- Line Emission
- Smooth Gradients
- Undo

### [Pro](https://z3y.gumroad.com/l/vrcm)

- Eraser
- Late join sync
- Subdivision
- Smooth and Accurate Sync
- Write on surface
- [Test World](https://vrchat.com/home/world/wrld_bd718520-e6ab-434b-a070-ee35b42f0d5f)

## Installation

### VRChat Creator Companion

- Add to VCC https://z3y.github.io/vpm-package-listing/
  - Requires VCC v2.1.2+
- Drag in the Prefab from the Packages folder in the Project Window

### Unity Package

- Download the Unity Package from [releases](https://github.com/z3y/VRCMarker/releases) and import into Unity
- Drag the Marker Prefab in your scene
- Adjust settings on the GameObject

### Package Manager

Alternatively you can install with the Unity Package Manager:

```
https://github.com/z3y/VRCMarker.git
```

- Requires UdonSharp v1 - [VCC](https://vcc.docs.vrchat.com/) Version

#

[Demo World](https://vrchat.com/home/world/wrld_df859907-113e-445b-9ec7-37c900c36c75)

![image](https://user-images.githubusercontent.com/33181641/235703413-ffb50822-af01-456f-a78a-7b9cf8307089.png)

![image](https://user-images.githubusercontent.com/33181641/194152197-a5647001-c29e-4231-a2f4-bf7858d2079a.png)

# How it works

The mesh for the trail renderer is created with the Unity Mesh API in Udon in the [MarkerTrail.cs](/Runtime/Scripts/MarkerTrail.cs) script. For the frequent updates to the mesh `MarkDynamic()` is enabled. The data we set are vertices, normals and triangles. This data is later used in the [Trail Renderer.shader](/Runtime/Shader/Trail%20Renderer.shader) to create a visible billboarding trail.

The trail consists of 2 segments: a line and a circle (4 verts for the quad + 3 vertex for a triangle).

The vertices (Vector3[]) in the Mesh refer to the OS (object space) position of the vertex. The mesh transform is set to (0,0,0) at start which means that OS and WS (world space) position will be the same. This avoids the cost of calculating it in Udon and vertex shader can skip OS to WS transform. The mesh bounds are set to infinite while drawing and are only properly calculated at the end of drawing.

The normals (Vector3[]) are used for encoding the position of the other end of the line, explained later. For the circles they are unused (0,0,0)

The triangles (int[]) represent the index of the vertex position array, from which we can get a position for a triangle vertex. The size of this array is always 3x of vertex[], each 3 in anti-clockwise order represent one triangle. This order is important for expanding our verticies later in the vertex shader and still have visible faces with back-face culling.

### The Circle

The circle is just a single triangle that has all 3 vertices positioned the same by Udon. It is expanded in the vertex shader to the proper position to create an equilateral triangle. We know where to move each vertex because we always set them in the same order in Udon and we can get this order with `SV_VertexID`. If we calculate `vertexID % 7` we get an uint in range [0, 6]. This will tell us the position where the vertex needs to get moved and whether it is a connecting line or a cirlce part of the trail (first 4 are for line, last 3 for the circle). Since we know where the center of our triangle is (the vertex position we set in udon) we can rotate around it so it always faces the camera position. This is done in 2 steps, where each one rotates it along 1 axis at a time, in WS to always be accurate on all cameras.

To create a circle inside the triangle we use a cutout shader, the outer parts are just discarded in the fragment shader. The center position is just passed from vertex to fragment, which makes it easy to draw a circle inside the triangle.

![Circle](/Images~/circle.png)

https://github.com/z3y/VRCMarker/assets/33181641/ffeddaa7-b91b-4993-9b17-b6727b0ccc8b

### The Line

The line has 4 vertices, 2 of which are positioned at the start of a trail line and 2 at the end. Each end knows where the position of the other end is because we encode this position data in the normals attribute in Udon (its just another vector3[] we can use it however we want in the shader). The 2 vertices on each side are expanded in the opposite directions which creates a visible line. We know where to move each vertex and keep it facing the camera, the same way as for the circle, from the vertexID. We also know the expand direction from the cross product of the vertex pos and the vertex pos at the other end (from vertex position and our "normals"). This is also billboarded in a way that allows it to rotate around the axis of the line while facing the camera position.

v1 = otherPos - vertexPos;
v2 = camPos - vertexPos;
v3 = +-cross(v1, v2)

![Line](/Images~/line.png)

https://github.com/z3y/VRCMarker/assets/33181641/61778357-57b1-47b9-bab9-f06d624218dc

### Circles + Lines

The 2 parts combined create a convincing trail:

https://github.com/z3y/VRCMarker/assets/33181641/158e8993-d2d4-4839-803a-7bfb2eff1d92

### Additional Info

Since all our positions are set along the line, it allows us to just expand it in the shader, we can have a property for the width that multiplies the expand to adjust the line thickness. If the width is set to 0 this is how our vertex data really looks like without any transforms, it just a thin invisible line. Because the math is much different for the 2 segments, the shader branches when calculating lines or circles to save performance.

The shader cant work as transparent because of sorting issues when having multiple trasparent objects, or trails with different colors intersecting each other. The cutout shader uses alpha to coverage to create an anti-aliased trail.

Property blocks are used for setting different color values for each pen without creating a new material. This still creates a new draw call for each trail but it makes the setup easy. Property blocks are also used for the marker mesh (not related to the trail), but in this case since the mesh is the same, it can be gpu instanced and property blocks can set different color properties which are declared as a instanced property in the shader.

### Gradients

Gradients are implemented entierly in the shader. We can easily create them because we have vertexID. Udon only needs to set gradient colors at start and we interpolate between them to always create a smooth color transition. It loops between the start and end color of the gradient in a sine wave over a set period of line segments
