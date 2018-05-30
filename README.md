ViewportPerspective
===================

![Screenshot](https://github.com/cecarlsen/ViewportPerspective/raw/master/Images/ViewportPerspectiveScreenshot.jpg)

Camera image effect for adjusting the viewport perspective using four corner handles.

Updated for Unity 2018.1.

Use cases
---------
- Align a video projection image with a physical canvas.
- Align multiple video projectors for a brighter image (1).
- Align two video projectors for stereography (2).
- Align multiple video projection images for a larger image (3).

(1) Windows users can use the Multidisplay feature. Mac users will have to use a single display output together with a multi-display adapter like the Matrox TrippleHead2Go.

(2) For stereoscopic projection you will need a separate stereo rendering method (like the Unity Virtual Reality feature), filters in your projectors and 3D glasses. Viewport Perspective simply aligns the two stereo images.

(3) Beware that this is not a comprehensive projection mapping tool. It is designed for aligning full projector images, not for slicing a source image to create complex mappings. Also, no edge blending.

Features
--------
- Supports Windows and OSX.
- Works well with multidisplay adapters like Matrox Dual- and TrippleHead2Go.
- Multidisplay (Windows player only)
- Runtime UI.
- Scriptable.
- Adaptable grid overlay.
- Saving runtime changes (optional)
- Edge antialiasing (optional)
- Examples




License
-------

Copyright (C) 2018 Carl Emil Carlsen

[GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html)
