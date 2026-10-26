namespace LlamaLogic.EpicTuningTime;

sealed class EpicTuningTimeApplication :
    Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new MainPage())
        {
            Height = 900,
            MinimumHeight = 480,
            MinimumWidth = 800,
            Title = "Epic Tuning Time",
            Width = 1440
        };
}