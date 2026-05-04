namespace SackranyPawn.Entities.Modules.ModuleComposition
{
    public interface ILateUpdateLimb
    {
        bool IsEnabled { get; }
        void OnLateUpdate(float deltaTime);
    }
}