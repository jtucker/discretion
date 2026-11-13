using System.Text.Json;
using Discretion.Models;

namespace Discretion.Services;

/// <summary>Stores the device owner's details, used as the Disclosing Party on new agreements.</summary>
public sealed class ProfileService
{
    private const string Key = "disclosing_party_profile";

    public PartyInfo Load()
    {
        var json = Preferences.Default.Get(Key, "");
        if (string.IsNullOrEmpty(json)) return new PartyInfo();
        try
        {
            return JsonSerializer.Deserialize(json, DiscretionJsonContext.Default.PartyInfo) ?? new PartyInfo();
        }
        catch (JsonException)
        {
            return new PartyInfo();
        }
    }

    public void Save(PartyInfo profile) =>
        Preferences.Default.Set(Key, JsonSerializer.Serialize(profile, DiscretionJsonContext.Default.PartyInfo));

    public bool IsComplete => !string.IsNullOrWhiteSpace(Load().Name);
}
