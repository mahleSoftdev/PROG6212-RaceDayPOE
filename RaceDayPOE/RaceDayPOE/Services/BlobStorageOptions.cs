namespace RaceDayPOE.Services;

//Settings for Azure Blob Storage, bound from the "AzureBlob" configuration section.
//Never commit a real connection string: use user secrets or the AzureBlob__ConnectionString environment variable.
public sealed class BlobStorageOptions
{
    public const string SectionName = "AzureBlob";

    //Storage account connection string. "UseDevelopmentStorage=true" targets the Azurite emulator.
    public string ConnectionString { get; set; } = "";

    //Container that holds all uploaded images (created on first upload if it does not exist).
    public string ContainerName { get; set; } = "raceday-images";
}
