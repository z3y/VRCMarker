import Image from "next/image"
import themeConfig from "../theme.config"

# Smoothing Modes

## Time

Move the last point slowly towards the pen over time.

<Image
  src={`${themeConfig.prefix}/smoothing-time.avif`}
  width={256}
  height={256}
/>

## Pulled String

Use a threshold over distance for moving the last point.

<Image
  src={`${themeConfig.prefix}/smoothing-pulled-string.avif`}
  width={256}
  height={256}
/>
