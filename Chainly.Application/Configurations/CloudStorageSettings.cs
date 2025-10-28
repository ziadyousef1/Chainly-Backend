namespace Chainly.Application.Configurations;

public class CloudStorageSettings
{
    public const string AzureStorage = "BlobStorage";
    public string ConnectionString { get; set; }
    public ContainersOptions Containers { get; set; }

}

public class ContainersOptions
{
    public string Avatars { get; set; }
}