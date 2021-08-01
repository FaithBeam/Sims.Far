namespace Sims.Far
{
    public interface IFar
    {
        Manifest Manifest { get; set; }
        int ManifestOffset { get; set; }
        string Signature { get; set; }
        int Version { get; set; }

        void Extract(ManifestEntry entry, string outputDirectory = "", bool preserveDirectories = true);
        void Extract(string fileName, string outputDirectory = "", bool preserveDirectories = true);
        byte[] GetBytes(string filename);
    }
}