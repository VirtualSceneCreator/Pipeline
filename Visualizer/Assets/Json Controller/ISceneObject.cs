
namespace Assets.Json_Controller
{
    public interface ISceneObject
    {
        string Specification { get; set; }
        string ObjectId { get; set; }
        string ObjectType { get; set; }
        string AssetName { get; set; }
        string Group { get; set; }
    }
}