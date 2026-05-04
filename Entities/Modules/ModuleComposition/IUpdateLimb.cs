namespace SackranyPawn.Entities.Modules.ModuleComposition
{
    public interface IUpdateLimb
    {
        bool IsEnabled { get; }
        void OnUpdate(float deltaTime);
    }
}