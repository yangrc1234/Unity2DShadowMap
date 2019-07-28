# Unity2DShadowMap
This repo shows a demo for the idea using shadowmap in 2d unity game.

![](Images/Preview.gif)

THIS IS ONLY A DEMO. It only contains very simple lighting behaviour, and it's not for making marvelous 2d graphics. It is only for providing the idea.

# Details
The method is of three steps.
1. Create shadow line mesh for each shadow caster. This doesn't need to be done every frame.
2. Render shadow mesh to each light's shadowmap. For point light, its shadowmap is 4 rows in the shadowmap texture(each row stands for a 90 degrees range).
3. Use shadow map to draw light. In the demo, a simple additive light is used. It samples the shadow map and does shadowing.

# References
[Fast 2D shadows in Unity using 1D shadow mapping](https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php)