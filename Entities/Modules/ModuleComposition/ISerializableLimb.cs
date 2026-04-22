namespace SackranyPawn.Entities.Modules.ModuleComposition
{
    public interface ISerializableLimb
    {
        public object[] Serialize();
        public void Deserialize(object[] data);
    }
}