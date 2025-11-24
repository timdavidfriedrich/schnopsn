namespace Schnopsn.Components.bummerl;

using Godot;
using System;
using Schnopsn.core.Utilities;

public partial class BummerlCounter : TextureProgressBar
{
	public override void _Ready()
    {
        Value = Rules.INITIAL_BUMMERL;
    }
}
