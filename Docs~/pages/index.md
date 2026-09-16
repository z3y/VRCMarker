import { Tabs } from "nextra/components"
import themeConfig from "../theme.config"
import { useState } from "react"
import { Cards, Card } from "nextra/components"
import Image from "next/image"

# VRCMarker

## About

VRCMarker uses a custom rounded line renderer, instead the Unity Line Renderer component which, provides better looking 3D lines for VR and works on both Quest and PC. The mesh is generated with Udon and shaders transform and billboard vertices in order to create a convincing line. You can read more about how it works [here](/how.mdx).

## [Test World](https://vrchat.com/home/world/wrld_bd718520-e6ab-434b-a070-ee35b42f0d5f)

## Install

### Free

<Tabs items={["VRChat Creator Companion", "Unity Package Manager"]}>
  <Tabs.Tab>
    Add the package listing <b>https://z3y.github.io/vpm-package-listing</b>
  </Tabs.Tab>
  <Tabs.Tab>
    Add the git url <b>{themeConfig.project.link + ".git"}</b>
  </Tabs.Tab>
</Tabs>

### Pro

The pro version is available on [Gumroad](https://z3y.gumroad.com/l/vrcm) and Booth.

Import the unity package. The imported files will appear in the Packages directory in the Project Window

![Image](/import-package.avif)
