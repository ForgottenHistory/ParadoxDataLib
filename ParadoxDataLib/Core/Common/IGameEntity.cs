namespace ParadoxDataLib.Core.Common
{
    public interface IGameEntity
    {
        string Id { get; }
        bool IsValid();
        void Validate();
    }
}