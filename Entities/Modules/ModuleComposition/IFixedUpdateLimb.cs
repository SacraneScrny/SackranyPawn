namespace SackranyPawn.Entities.Modules.ModuleComposition
{
    public interface IFixedUpdateLimb
    {
        bool IsEnabled { get; }
        void OnFixedUpdate(float deltaTime);
    }
}