using Content.Shared.Language;
using Content.Shared.Language.Systems;
using Content.Shared.Mind.Components;
using Robust.Shared.Console;

namespace Content.Client.Language.Systems;

/// <summary>
///   Client-side language system.
///
///   Unlike the server, the client is not aware of other entities' languages; it's only notified about the entity that it posesses.
///   Due to that, this system stores such information in a static manner.
/// </summary>
public sealed class LanguageSystem : SharedLanguageSystem
{
    /// <summary>
    ///   The current language of the entity currently possessed by the player.
    /// </summary>
    public string CurrentLanguage { get; private set; } = default!;
    /// <summary>
    ///   The list of languages the currently possessed entity can speak.
    /// </summary>
    public List<string> SpokenLanguages { get; private set; } = new();
    /// <summary>
    ///   The list of languages the currently possessed entity can understand.
    /// </summary>
    public List<string> UnderstoodLanguages { get; private set; } = new();

    public Action<(string current, List<string> spoken, List<string> understood)>? LanguagesUpdatedHook;

    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<LanguagesUpdatedMessage>(OnLanguagesUpdated);
    }

    /// <summary>
    ///   Sends a network request to the server to update this system's state.
    ///   The server may ignore the said request if the player is not possessing an entity.
    /// </summary>
    public void RequestStateUpdate()
    {
        RaiseNetworkEvent(new RequestLanguagesMessage());
    }

    public void RequestSetLanguage(LanguagePrototype language)
    {
        // May cause some minor desync...
        if (language.ID == CurrentLanguage)
            return;

        // (This is dumb. This is very dumb. It should be a message instead.)
        _consoleHost.ExecuteCommand("lsselectlang " + language.ID);

        // So to reduce the probability of desync, we replicate the change locally too
        if (SpokenLanguages.Contains(language.ID))
            CurrentLanguage = language.ID;
    }

    private void OnLanguagesUpdated(LanguagesUpdatedMessage message)
    {
        CurrentLanguage = message.CurrentLanguage;
        SpokenLanguages = message.Spoken;
        UnderstoodLanguages = message.Understood;

        // Pleeease do not mutate it inside the hook, or the universe will crash and collapse and I will come to your house at 3 am and then the police will never find your body
        LanguagesUpdatedHook?.Invoke((CurrentLanguage, SpokenLanguages, UnderstoodLanguages));
    }
}
