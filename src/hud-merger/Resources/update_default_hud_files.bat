@ECHO OFF
SET VPK="C:\Program Files (x86)\Steam\steamapps\common\Team Fortress 2\bin\vpk.exe"
SET TF2MISC="C:\Program Files (x86)\Steam\steamapps\common\Team Fortress 2\tf\tf2_misc_dir.vpk"

MKDIR HUD
CD HUD

MKDIR resource
MKDIR scripts

VPK x %TF2MISC% "resource/clientscheme.res"
VPK x %TF2MISC% "scripts/hudanimations_manifest.txt"
VPK x %TF2MISC% "scripts/hudlayout.res"

CD ..