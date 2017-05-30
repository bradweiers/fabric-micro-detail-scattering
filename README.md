# Micro Detail Fabric Shader
Requires Unity 5.4+

This repository will be moved to the Unity-Technologies GitHub organization soon

![Fabric Shader](game-view.PNG?raw=true "Fabric Shader")

## Material Settings

![Material UI](UI.PNG?raw=true "Material UI")

### Micro details
- Encompass a variety of features on the micro level:
    - Micro light reflection/absorbtion
    - Micro shadows
    - Micro subsurface scattering - expensive (extra matrix operation and normal convolution)
- Disabling "Micro Details" disables all of the above features.
- All micro features are derived from a small (64x64 is fine), tileable, fabric pattern map, which should be created as a normal map, as well as an ambient occlusion map stored in the alpha. 
- Notes:
    - Micro Shadow Strength: Blends from no shadows to small shadows based on slider, this calculation is based on an approximation from the AO
    - Micro Detail Strength: Blends from no AO to full AO before doing more behind the scenes work with the term calculated from this.

### Fabric Scattering
This is a cheap subsurface scattering approximation achieved by simple wrap lighting and mixed with a scatter color term.

Differently calculated from micro subsurface, this term is meant to have more of an impact on the "macro" scattering that occurs in fabric, while the micro scattering simulates the scattering involved on a single local yarn.

### Future Work
There is a lot of work done with the AO to enhance rim lighting. We employ a technique used by Naughty Dog in which there is an "AO Fresnel"--that is the AO falls off in intensity at grazing angles. We also drop the micro detail strength at grazing angles.

---

Authored by John Parsaie under supervision of Andrew Maneri and Brad Weiers at Unity Technologies