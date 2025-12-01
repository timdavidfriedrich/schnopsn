namespace Schnopsn.components.start_menu;

using Godot;
using Schnopsn.core;

public partial class StartMenuCard : AspectRatioContainer
{
    [Signal]
    public delegate void ReadyToStartEventHandler();

    [Export]
    private AnimationPlayer _bannerGuyAnimationPlayer;
    
    private bool _isReadyToStart = false;

    public override void _Ready()
    {
        PlayAnimations();
    }

    private async void PlayAnimations()
    {
        _bannerGuyAnimationPlayer.Play(BannerGuyAnimations.ShowBanner);

        await ToSignal(GetTree().CreateTimer(1.7f), "timeout");
        AudioManager.Instance?.PlayFlipSound();
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        AudioManager.Instance?.PlayFlightSound();

        await ToSignal(_bannerGuyAnimationPlayer, "animation_finished");
        _bannerGuyAnimationPlayer.Play(BannerGuyAnimations.AfterShow);
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        EmitSignal(SignalName.ReadyToStart);
    }

}

file record BannerGuyAnimations(StringName Value)
{
    public static implicit operator StringName(BannerGuyAnimations a) => a.Value;
    public static readonly BannerGuyAnimations ShowBanner = new("show_banner");
    public static readonly BannerGuyAnimations AfterShow = new("after_show");
}