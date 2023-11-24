# PBRCreator
Unity script to facilitate the use of PBR materials from the Internet.

1- Copy the folder in your Assets.

2- Import texture maps downloaded from:
Substance Painter (URP template)
withpoly.com
polyhaven.com
3dtextures.me
ambientcg.com

3- Select the set in the assets panel, right click for the menu options. Create a material with standard or triplanar shader (built-in pipeline).

It looks for common map names, sets the normal, converts roughness to smoothness, and combines metallic and smoothness if possible.  

Based on EasyTextureAssign by Cody Anderson and the Metallic-Smoothness script by Todd Gillissie.