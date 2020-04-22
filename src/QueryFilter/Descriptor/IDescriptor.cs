namespace QueryFilter
{
    public interface IDescriptor
    {
        void Deserialize(string source);
        string Serialize();
    }
}
